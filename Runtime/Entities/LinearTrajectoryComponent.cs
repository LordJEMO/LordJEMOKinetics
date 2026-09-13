#if KINEMATICS_ENTITIES
using Unity.Entities;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     ECS wrapper for the <see cref="LinearTrajectory"/> motion law. One component per trajectory
    ///     kind (<c>IComponentData</c> cannot be generic): copy this file per new implementation.
    /// </summary>
    public struct LinearTrajectoryComponent : IComponentData
    {
        public LinearTrajectory Value;
    }
}
#endif
