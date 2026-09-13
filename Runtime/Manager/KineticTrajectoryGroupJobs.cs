using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Burst / Jobs group backend: the advance is a parallel
    ///     <see cref="KineticEntryAdvanceJob{TTrajectory, TContext}"/> (via <see cref="KineticBatch"/>)
    ///     over the group's one <see cref="KineticTrajectoryGroupBase{TTrajectory, TContext}.Trajectory"/>,
    ///     and the transform write is a <c>[BurstCompile]</c>
    ///     <see cref="KineticTransformWriteJob{TContext}"/> over a <see cref="TransformAccessArray"/>
    ///     holding only the entries that have a transform <em>and</em> transform-sync on (rebuilt on
    ///     structural change).
    ///
    ///     <see cref="KineticTrajectoryGroupBase{TTrajectory, TContext}.Tick"/> completes its advance
    ///     job before returning, so reading entry status between the tick and
    ///     <see cref="ApplyTransforms"/> is safe.
    /// </summary>
    public sealed class KineticTrajectoryGroupJobs<TTrajectory, TContext> : KineticTrajectoryGroupBase<TTrajectory, TContext>
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        private TransformAccessArray transformAccess;
        private NativeList<int> taaToSlot;                 // TransformAccessArray index -> physical slot
        private readonly List<Transform> rebuildBuffer = new List<Transform>();
        private bool taaDirty = true;

        public KineticTrajectoryGroupJobs(in TTrajectory trajectory, int initialCapacity = 64)
            : base(in trajectory, initialCapacity)
        {
            taaToSlot = new NativeList<int>(initialCapacity, Allocator.Persistent);
        }

        protected override void OnStructuralChange()
        {
            taaDirty = true;
        }

        protected override void Advance(float deltaTime)
        {
            KineticBatch.Run<TTrajectory, TContext>(Trajectory, entries.AsArray(), deltaTime);
        }

        public override void ApplyTransforms()
        {
            if (entries.Length == 0)
            {
                return;
            }

            if (taaDirty)
            {
                RebuildTransformAccess();
                taaDirty = false;
            }

            if (transformAccess.length == 0)
            {
                return;
            }

            KineticTransformWriteJob<TContext> job = new KineticTransformWriteJob<TContext>
            {
                Entries = entries.AsArray(),
                TaaToSlot = taaToSlot.AsArray(),
            };

            job.Schedule(transformAccess).Complete();
        }

        private void RebuildTransformAccess()
        {
            taaToSlot.Clear();
            rebuildBuffer.Clear();

            for (int slot = 0; slot < transforms.Count; slot++)
            {
                Transform target = transforms[slot];
                if (target == null || writeTransform[slot] == 0)
                {
                    continue;
                }

                taaToSlot.Add(slot);
                rebuildBuffer.Add(target);
            }

            Transform[] live = rebuildBuffer.ToArray();
            if (!transformAccess.isCreated)
            {
                transformAccess = new TransformAccessArray(live);
            }
            else
            {
                transformAccess.SetTransforms(live);
            }
        }

        public override void Dispose()
        {
            if (transformAccess.isCreated)
            {
                transformAccess.Dispose();
            }

            if (taaToSlot.IsCreated)
            {
                taaToSlot.Dispose();
            }

            base.Dispose();
        }
    }

    /// <summary>
    ///     Writes the resolved pose of each synced entry onto its <see cref="Transform"/> off the main
    ///     thread. <see cref="TaaToSlot"/> maps the <see cref="TransformAccessArray"/> index to the
    ///     physical slot in <see cref="Entries"/>. Independent of the trajectory type — it only reads
    ///     <see cref="KineticEntry{TContext}.State"/>.
    /// </summary>
    [BurstCompile]
    public struct KineticTransformWriteJob<TContext> : IJobParallelForTransform
        where TContext : unmanaged, IKineticInput
    {
        [ReadOnly] public NativeArray<KineticEntry<TContext>> Entries;
        [ReadOnly] public NativeArray<int> TaaToSlot;

        public void Execute(int index, TransformAccess transform)
        {
            KineticPlayback state = Entries[TaaToSlot[index]].State;
            transform.position = state.Position;
            transform.rotation = state.Rotation;
        }
    }
}
