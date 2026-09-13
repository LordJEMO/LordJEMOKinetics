using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The per-body inputs a motion law is evaluated against — the modern replacement for the
    ///     original <c>IKineticPoint</c>. Kept as an interface so it can be widened into richer
    ///     contexts (a waypoint list for a spline, a moving-target reference for a homing law, …)
    ///     without changing <see cref="IKineticTrajectory{TContext}"/>; a trajectory names the
    ///     concrete context type it understands as its type argument.
    ///
    ///     Implementations MUST be <c>unmanaged struct</c>s so they satisfy
    ///     <c>where TContext : unmanaged, IKineticInput</c> and ride inside a
    ///     <see cref="Unity.Collections.NativeArray{T}"/> / <c>IComponentData</c>. In generic code the
    ///     members below are constrained calls on the struct type parameter — no boxing, Burst-safe.
    /// </summary>
    public interface IKineticInput
    {
        /// <summary>Spawn / start position.</summary>
        float3 Origin { get; set; }

        /// <summary>Destination / aim point. Path-style contexts expose their last point here.</summary>
        float3 Target { get; set; }
    }
}
