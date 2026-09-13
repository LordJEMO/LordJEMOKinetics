using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The emitter of one registered key. Internal so gameplay code holding an
    ///     <see cref="IKineticEmitter{TInput}"/> cannot cast its way to the group.
    /// </summary>
    internal sealed class KineticEmitter<TTrajectory, TInput> : IKineticEmitter<TInput>
        where TInput : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TInput>
    {
        public KineticEmitter(IKineticTrajectoryGroup<TTrajectory, TInput> group)
        {
            Group = group;
        }

        public IKineticTrajectoryGroup<TTrajectory, TInput> Group { get; }

        public KineticObject Add(in TInput input, Transform transform = null, bool writeTransform = true)
        {
            return Group.Add(in input, transform, writeTransform);
        }
    }
}
