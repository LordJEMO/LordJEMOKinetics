using System;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Straight-line motion at a fixed velocity: <c>Position = Origin + Velocity * time</c>. The
    ///     rule carries no direction of its own — everything about where the body goes lives in the
    ///     per-entry <see cref="KineticVelocityInput"/>. Launch-and-let-fly bodies (bullets,
    ///     debris) whose lifetime is decided by a hit or a cull, not by a destination.
    ///
    ///     <see cref="LifetimeSeconds"/> is optional:
    ///     <list type="bullet">
    ///       <item><c>&gt; 0</c> — bounded. <see cref="Duration"/> is the lifetime, the body clamps at
    ///       <c>Origin + Velocity * Lifetime</c> and reports <see cref="KineticStatus.Completed"/>.</item>
    ///       <item><c>&lt;= 0</c> — unbounded. <see cref="Duration"/> is <c>+∞</c>, so the body never
    ///       completes and <see cref="IKineticClip{TContext}.NormalizedTime"/> stays <c>0</c>. The
    ///       <see cref="IKineticClip{TContext}"/> members are still present but inert — test
    ///       <see cref="LifetimeSeconds"/>, not the interface, when it matters.</item>
    ///     </list>
    /// </summary>
    [Serializable]
    public struct ConstantVelocityTrajectory : IKineticClip<KineticVelocityInput>
    {
        [Min(0f)] [Tooltip("Seconds before the body completes. 0 = never completes (travels forever).")]
        public float LifetimeSeconds;

        public ConstantVelocityTrajectory(float lifetimeSeconds)
        {
            LifetimeSeconds = lifetimeSeconds;
        }

        /// <summary>Travels forever — never reports <see cref="KineticStatus.Completed"/>.</summary>
        public static ConstantVelocityTrajectory Forever()
        {
            return new ConstantVelocityTrajectory(0f);
        }

        /// <summary>Travels for <paramref name="seconds"/>, then clamps and completes.</summary>
        public static ConstantVelocityTrajectory ForSeconds(float seconds)
        {
            return new ConstantVelocityTrajectory(seconds);
        }
        public float Duration(in KineticVelocityInput context)
        {
            if (LifetimeSeconds > KineticMath.Epsilon)
            {
                return LifetimeSeconds;
            }

            return float.PositiveInfinity;
        }

        public float NormalizedTime(in KineticVelocityInput context, float time)
        {
            // time / +inf is 0, so an unbounded body simply never progresses — no branch needed.
            return math.saturate(time / Duration(in context));
        }

        public KineticOutput Evaluate(in KineticVelocityInput context, float time)
        {
            float duration = Duration(in context);

            // +inf makes every case fall out: min(t, inf) == t, t / inf == 0, t >= inf is false.
            float elapsed = math.min(time, duration);
            float3 direction = math.normalizesafe(context.Velocity, float3.zero);

            return new KineticOutput
            {
                Position = context.Origin + (context.Velocity * elapsed),
                Direction = direction,
                Rotation = KineticMath.LookRotation(direction),
                NormalizedTime = math.saturate(time / duration),
                Status = time >= duration ? KineticStatus.Completed : KineticStatus.Playing,
            };
        }
    }
}
