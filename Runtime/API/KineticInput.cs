using System;
using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The plain two-anchor <see cref="IKineticInput"/>: a start and an end. What
    ///     <see cref="LinearTrajectory"/> (and any point-to-point law) is evaluated against. Endpoints
    ///     live here, not on the trajectory, so a body can be retargeted mid-flight by mutating the
    ///     context while the injected trajectory stays put.
    ///
    ///     Blittable so it rides in a <see cref="Unity.Collections.NativeArray{T}"/> and an
    ///     <c>IComponentData</c> alongside the trajectory.
    /// </summary>
    [Serializable]
    public struct KineticInput : IKineticInput
    {
        /// <summary>Spawn / start position. <see cref="KineticMobile"/> can fill this from its transform at play time.</summary>
        public float3 Origin;

        /// <summary>Destination / aim point.</summary>
        public float3 Target;

        public KineticInput(float3 origin, float3 target)
        {
            Origin = origin;
            Target = target;
        }

        // Explicit implementations so the public fields above stay Unity-serialized (auto-property
        // backing fields would not be) while still satisfying the interface for generic call sites.
        // The setters let generic manager code retarget an entry (a.Context.Target = ...) through the
        // constraint without knowing the concrete context type — a constrained call, no boxing.
        float3 IKineticInput.Origin
        {
            readonly get => Origin;
            set => Origin = value;
        }

        float3 IKineticInput.Target
        {
            readonly get => Target;
            set => Target = value;
        }
    }
}
