#if KINEMATICS_ENTITIES
using Unity.Entities;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     ECS wrapper for the per-body runtime. Advanced in place by
    ///     <see cref="KineticMovementSystem"/> each frame, exactly as
    ///     <see cref="KineticUpdateJob"/> advances its array elements.
    /// </summary>
    public struct KineticPlaybackComponent : IComponentData
    {
        public KineticPlayback Value;
    }
}
#endif
