using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Shared constants and helpers for motion laws and adapters. Intentionally NOT
    ///     <c>[BurstCompile]</c>-decorated: like the grid system's <c>BlockGridCellPlacement</c>,
    ///     keeping the shared math running under plain IL is what lets a parity test compare the Mono
    ///     path against the Burst job and catch a stale Burst toolchain.
    /// </summary>
    public static class KineticMath
    {
        /// <summary>
        ///     Guard for divide-by-zero and "arrived" comparisons. A compile-time literal Burst can
        ///     fold — deliberately not <see cref="LordJEMO.Utilities.MathHelper.Epsilon"/>, whose
        ///     value is produced by a method that takes an enum argument. 1e-5 sits well below any
        ///     meaningful sub-frame time or sub-millimetre distance.
        /// </summary>
        public const float Epsilon = 1.0e-5f;

        /// <summary>
        ///     True when <paramref name="direction"/> is long enough to derive an orientation from
        ///     (i.e. not effectively zero).
        /// </summary>
        public static bool HasDirection(float3 direction)
        {
            return math.lengthsq(direction) > Epsilon;
        }

        /// <summary>
        ///     Orientation facing <paramref name="direction"/> (world up), or
        ///     <c>quaternion.identity</c> when there is no direction to face. Always returns a valid
        ///     quaternion.
        /// </summary>
        public static quaternion LookRotation(float3 direction)
        {
            if (HasDirection(direction))
            {
                return quaternion.LookRotationSafe(direction, math.up());
            }

            return quaternion.identity;
        }
    }
}
