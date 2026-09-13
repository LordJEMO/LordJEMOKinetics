using System;
using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     A point-and-velocity <see cref="IKineticInput"/>: where the body starts and how fast it
    ///     is going, with no destination. What <see cref="ConstantVelocityTrajectory"/> (and any
    ///     launch-and-let-fly law) is evaluated against.
    ///
    ///     Blittable so it rides in a <see cref="Unity.Collections.NativeArray{T}"/> alongside the
    ///     trajectory.
    /// </summary>
    [Serializable]
    public struct KineticVelocityInput : IKineticInput
    {
        /// <summary>Launch point.</summary>
        public float3 Origin;

        /// <summary>World-space velocity in units per second — direction and speed in one vector.</summary>
        public float3 Velocity;

        public KineticVelocityInput(float3 origin, float3 velocity)
        {
            Origin = origin;
            Velocity = velocity;
        }

        /// <summary>Speed in units per second (the magnitude of <see cref="Velocity"/>).</summary>
        public readonly float Speed => math.length(Velocity);

        // Explicit implementations so the public fields above stay Unity-serialized while still
        // satisfying the interface for generic call sites.
        float3 IKineticInput.Origin
        {
            readonly get => Origin;
            set => Origin = value;
        }

        // This context has no destination. Target is derived as the point one second ahead so the
        // shared IKineticInput contract still holds; setting it re-derives Velocity from the aim
        // point, i.e. "reach there in one second". Laws that need a real destination should use
        // KineticInput instead.
        float3 IKineticInput.Target
        {
            readonly get => Origin + Velocity;
            set => Velocity = value - Origin;
        }
    }
}
