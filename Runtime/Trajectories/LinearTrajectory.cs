using System;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Straight-line motion from <see cref="KineticInput.Origin"/> to
    ///     <see cref="KineticInput.Target"/>. The one motion law in the first pass; it exists to
    ///     prove the <see cref="IKineticTrajectory{TContext}"/> seam. It is a bounded law, so it
    ///     implements <see cref="IKineticClip{TContext}"/> (it has a timeline). Add a new law by
    ///     copying this file, not by touching the animator or the jobs.
    ///
    ///     Give it a fixed <see cref="DurationSeconds"/>, or leave that at 0 and set
    ///     <see cref="SpeedUnitsPerSecond"/> to travel at a constant speed regardless of distance.
    /// </summary>
    [Serializable]
    public struct LinearTrajectory : IKineticClip<KineticInput>
    {
        [Min(0f)] [Tooltip("Travel time in seconds. If 0, duration is derived from Speed and the Origin→Target distance.")]
        public float DurationSeconds;

        [Min(0f)] [Tooltip("Constant travel speed in units/second. Used only when Duration is 0.")]
        public float SpeedUnitsPerSecond;

        public LinearTrajectory(float durationSeconds, float speedUnitsPerSecond)
        {
            DurationSeconds = durationSeconds;
            SpeedUnitsPerSecond = speedUnitsPerSecond;
        }

        /// <summary>A linear trajectory completed over <paramref name="seconds"/>, independent of distance.</summary>
        public static LinearTrajectory OverTime(float seconds)
        {
            return new LinearTrajectory(seconds, 0f);
        }

        /// <summary>A linear trajectory travelled at a constant <paramref name="unitsPerSecond"/>.</summary>
        public static LinearTrajectory AtSpeed(float unitsPerSecond)
        {
            return new LinearTrajectory(0f, unitsPerSecond);
        }
        public float Duration(in KineticInput context)
        {
            if (DurationSeconds > KineticMath.Epsilon)
            {
                return DurationSeconds;
            }

            if (SpeedUnitsPerSecond > KineticMath.Epsilon)
            {
                return math.distance(context.Origin, context.Target) / SpeedUnitsPerSecond;
            }

            return 0f;
        }

        public float NormalizedTime(in KineticInput context, float time)
        {
            float duration = Duration(in context);
            return duration > KineticMath.Epsilon ? math.saturate(time / duration) : 1f;
        }

        public KineticOutput Evaluate(in KineticInput context, float time)
        {
            float normalized = NormalizedTime(in context, time);

            float3 delta = context.Target - context.Origin;
            float3 direction = math.normalizesafe(delta, float3.zero);

            return new KineticOutput
            {
                Position = math.lerp(context.Origin, context.Target, normalized),
                Direction = direction,
                Rotation = KineticMath.LookRotation(direction),
                NormalizedTime = normalized,
                Status = normalized >= 1f ? KineticStatus.Completed : KineticStatus.Playing,
            };
        }
    }
}
