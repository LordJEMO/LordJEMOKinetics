using System;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Lightweight façade over one entry. Holds no entry data itself — every member forwards to the
    ///     group that owns the entry, which stays hidden. Returned by <see cref="IKineticEmitter{TInput}.Add"/>;
    ///     keep <see cref="Handle"/> if you need a reference that outlives the struct. A <c>default</c>
    ///     value is the invalid object.
    /// </summary>
    public readonly struct KineticObject : IKineticObject
    {
        private readonly IKineticTrajectoryGroup group;
        private readonly KineticHandle handle;

        public KineticObject(IKineticTrajectoryGroup group, KineticHandle handle)
        {
            this.group = group;
            this.handle = handle;
        }

        public KineticHandle Handle => handle;

        public bool IsValid => group != null && group.IsValid(handle);

        public KineticPlayback State => group != null ? group.GetState(handle) : default;

        public KineticStatus Status => State.Status;

        public Transform Transform => group != null ? group.GetTransform(handle) : null;

        public TInput GetInput<TInput>()
            where TInput : unmanaged, IKineticInput
        {
            if (group == null)
            {
                return default;
            }

            return Typed<TInput>().GetContext(handle);
        }

        public void SetInput<TInput>(in TInput input)
            where TInput : unmanaged, IKineticInput
        {
            if (group == null)
            {
                return;
            }

            Typed<TInput>().SetContext(handle, in input);
        }

        public void SetOrigin(float3 origin)
        {
            group?.SetOrigin(handle, origin);
        }

        public void SetTarget(float3 target)
        {
            group?.SetTarget(handle, target);
        }

        public void SetWriteTransform(bool writeTransform)
        {
            group?.SetWriteTransform(handle, writeTransform);
        }

        public void Pause()
        {
            group?.Pause(handle);
        }

        public void Resume()
        {
            group?.Resume(handle);
        }

        public void Replay()
        {
            group?.Replay(handle);
        }

        public void Remove()
        {
            group?.Remove(handle);
        }

        private IKineticTrajectoryGroup<TInput> Typed<TInput>()
            where TInput : unmanaged, IKineticInput
        {
            if (group is IKineticTrajectoryGroup<TInput> typed)
            {
                return typed;
            }

            throw new ArgumentException($"This entry takes {group.InputType.Name} input, not {typeof(TInput).Name}.");
        }
    }
}
