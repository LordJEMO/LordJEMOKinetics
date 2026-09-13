#if KINEMATICS_ENTITIES
using Unity.Entities;

namespace LordJEMO.Kinematics
{
    /// <summary>ECS wrapper for the endpoints a motion law is evaluated against. Shared by every trajectory kind.</summary>
    public struct KineticInputComponent : IComponentData
    {
        public KineticInput Value;
    }
}
#endif
