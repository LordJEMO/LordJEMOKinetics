using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Storage + slotmap + structural operations shared by both group backends
    ///     (<see cref="KineticTrajectoryGroupMain{TTrajectory, TContext}"/>,
    ///     <see cref="KineticTrajectoryGroupJobs{TTrajectory, TContext}"/>). One rule
    ///     (<see cref="Trajectory"/>); many <see cref="KineticEntry{TContext}"/> in one dense list,
    ///     each with a borrowed transform and a transform-sync flag.
    ///
    ///     Subclasses supply only <see cref="Advance"/> and <see cref="ApplyTransforms"/>.
    /// </summary>
    public abstract class KineticTrajectoryGroupBase<TTrajectory, TContext> : IKineticTrajectoryGroup<TTrajectory, TContext>
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        private TTrajectory trajectory;

        /// <summary>Dense entry store, index = physical slot. Parallel to <see cref="transforms"/>, <see cref="writeTransform"/>, <see cref="slotToId"/>.</summary>
        protected NativeList<KineticEntry<TContext>> entries;

        /// <summary>Per-slot borrowed transform (may be <c>null</c>). The group never creates or destroys these.</summary>
        protected readonly List<Transform> transforms = new List<Transform>();

        /// <summary>Per-slot flag: 1 = <see cref="ApplyTransforms"/> writes this entry's transform, 0 = leave it.</summary>
        protected NativeList<byte> writeTransform;

        // Entry slotmap: stable id <-> physical slot, with a recycle version per id.
        private NativeList<int> slotToId;
        private NativeList<int> idToSlot;
        private NativeList<int> versions;
        private NativeList<int> freeIds;

        protected KineticTrajectoryGroupBase(in TTrajectory trajectory, int initialCapacity)
        {
            this.trajectory = trajectory;

            int capacity = math.max(initialCapacity, 1);
            entries = new NativeList<KineticEntry<TContext>>(capacity, Allocator.Persistent);
            writeTransform = new NativeList<byte>(capacity, Allocator.Persistent);
            slotToId = new NativeList<int>(capacity, Allocator.Persistent);
            idToSlot = new NativeList<int>(capacity, Allocator.Persistent);
            versions = new NativeList<int>(capacity, Allocator.Persistent);
            freeIds = new NativeList<int>(capacity, Allocator.Persistent);
        }

        public TTrajectory Trajectory => trajectory;

        public void SetTrajectory(in TTrajectory value)
        {
            trajectory = value;
        }

        public System.Type InputType => typeof(TContext);

        public int Count => entries.Length;

        #region entries

        public KineticObject Add(in TContext context, Transform transform = null, bool writeTransformFlag = true)
        {
            KineticAnimator<TTrajectory, TContext> animator = default;
            animator.Trajectory = trajectory;
            animator.Context = context;
            animator.Reset();

            KineticEntry<TContext> entry = default;
            entry.Context = animator.Context;
            entry.State = animator.State;

            int slot = entries.Length;
            entries.Add(entry);
            transforms.Add(transform);
            writeTransform.Add(writeTransformFlag ? (byte)1 : (byte)0);

            int id;
            if (freeIds.Length > 0)
            {
                id = freeIds[freeIds.Length - 1];
                freeIds.RemoveAt(freeIds.Length - 1);
                idToSlot[id] = slot;
            }
            else
            {
                id = idToSlot.Length;
                idToSlot.Add(slot);
                versions.Add(0);
            }

            slotToId.Add(id);

            OnStructuralChange();
            return new KineticObject(this, new KineticHandle(id, versions[id]));
        }

        public bool IsValid(KineticHandle handle)
        {
            return ResolveSlot(handle) >= 0;
        }

        public KineticPlayback GetState(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            return slot < 0 ? default : entries[slot].State;
        }

        public TContext GetContext(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            return slot < 0 ? default : entries[slot].Context;
        }

        public void SetContext(KineticHandle handle, in TContext context)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];
            entry.Context = context;
            entries[slot] = entry;
        }

        public Transform GetTransform(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            return slot < 0 ? null : transforms[slot];
        }

        public void SetOrigin(KineticHandle handle, float3 origin)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];
            entry.Context.Origin = origin;   // constrained call on TContext — no boxing
            entries[slot] = entry;
        }

        public void SetTarget(KineticHandle handle, float3 target)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];
            entry.Context.Target = target;   // constrained call on TContext — no boxing
            entries[slot] = entry;
        }

        public void SetWriteTransform(KineticHandle handle, bool writeTransformFlag)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            byte value = writeTransformFlag ? (byte)1 : (byte)0;
            if (writeTransform[slot] == value)
            {
                return;
            }

            writeTransform[slot] = value;
            OnStructuralChange();   // the Jobs backend's TransformAccessArray membership changed
        }

        public void Pause(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];
            if (entry.State.Status == KineticStatus.Playing)
            {
                entry.State.Status = KineticStatus.Paused;
                entries[slot] = entry;
            }
        }

        public void Resume(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];
            if (entry.State.Status == KineticStatus.Paused)
            {
                entry.State.Status = KineticStatus.Playing;
                entries[slot] = entry;
            }
        }

        public void Replay(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            KineticEntry<TContext> entry = entries[slot];

            KineticAnimator<TTrajectory, TContext> animator = default;
            animator.Trajectory = trajectory;
            animator.Context = entry.Context;
            animator.State = entry.State;
            animator.Reset();

            entry.Context = animator.Context;
            entry.State = animator.State;
            entries[slot] = entry;
        }

        public void Remove(KineticHandle handle)
        {
            int slot = ResolveSlot(handle);
            if (slot < 0)
            {
                return;
            }

            int last = entries.Length - 1;
            if (slot != last)
            {
                entries[slot] = entries[last];
                transforms[slot] = transforms[last];
                writeTransform[slot] = writeTransform[last];

                int movedId = slotToId[last];
                slotToId[slot] = movedId;
                idToSlot[movedId] = slot;
            }

            entries.RemoveAt(last);
            transforms.RemoveAt(last);
            writeTransform.RemoveAt(last);
            slotToId.RemoveAt(last);

            idToSlot[handle.Id] = -1;
            versions[handle.Id] = versions[handle.Id] + 1;
            freeIds.Add(handle.Id);

            OnStructuralChange();
        }

        #endregion

        #region per-frame

        public void Tick(float deltaTime)
        {
            if (entries.Length == 0)
            {
                return;
            }

            Advance(deltaTime);
        }

        public abstract void ApplyTransforms();

        #endregion

        /// <summary>Advance every entry in <see cref="entries"/> by <paramref name="deltaTime"/> under <see cref="Trajectory"/>.</summary>
        protected abstract void Advance(float deltaTime);

        /// <summary>Called after every <see cref="Add"/> / <see cref="Remove"/> / write-flag flip so a backend can rebuild derived structures (e.g. a <c>TransformAccessArray</c>).</summary>
        protected virtual void OnStructuralChange()
        {
        }

        private int ResolveSlot(KineticHandle handle)
        {
            if (handle.Id < 0 || handle.Id >= idToSlot.Length)
            {
                return -1;
            }

            if (versions[handle.Id] != handle.Version)
            {
                return -1;
            }

            return idToSlot[handle.Id];
        }

        public virtual void Dispose()
        {
            DisposeIfCreated(ref entries);
            DisposeIfCreated(ref writeTransform);
            DisposeIfCreated(ref slotToId);
            DisposeIfCreated(ref idToSlot);
            DisposeIfCreated(ref versions);
            DisposeIfCreated(ref freeIds);
            transforms.Clear();
        }

        private static void DisposeIfCreated<T>(ref NativeList<T> list) where T : unmanaged
        {
            if (list.IsCreated)
            {
                list.Dispose();
            }
        }
    }
}
