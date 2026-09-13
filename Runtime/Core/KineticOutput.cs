using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The result of evaluating an <see cref="IKineticTrajectory{TContext}"/> at one instant. A
    ///     pure value — <see cref="IKineticTrajectory{TContext}.Evaluate"/> returns it, callers copy
    ///     the parts they need into a <see cref="KineticPlayback"/> (or a transform). Kept separate from
    ///     <see cref="KineticPlayback"/> so a trajectory stays free of the running clock and can be
    ///     sampled at arbitrary lookahead times (aiming, prediction, gizmos).
    /// </summary>
    public struct KineticOutput : IKineticOutPut
    {
        /// <summary>World-space position at the evaluated time.</summary>
        public float3 Position;

        /// <summary>Unit travel direction at the evaluated time; <c>float3.zero</c> when undefined.</summary>
        public float3 Direction;

        /// <summary>
        ///     Orientation at the evaluated time. The trajectory decides it — <see cref="LinearTrajectory"/>
        ///     faces <see cref="Direction"/>; a future law may bank or roll independently. Always a
        ///     valid quaternion (<c>quaternion.identity</c> when there is no direction to face).
        /// </summary>
        public quaternion Rotation;

        /// <summary>
        ///     Evaluated time mapped to <c>[0, 1]</c> over the trajectory's duration. Only meaningful
        ///     when the trajectory is an <see cref="IKineticClip{TContext}"/>; an unbounded law
        ///     leaves this <c>0</c>.
        /// </summary>
        public float NormalizedTime;

        /// <summary>Lifecycle status. From a pure evaluation this is only ever <see cref="KineticStatus.Playing"/> or <see cref="KineticStatus.Completed"/>.</summary>
        public KineticStatus Status;

        readonly float3 IKineticOutPut.Position => Position;
        readonly float3 IKineticOutPut.Direction => Direction;
        readonly quaternion IKineticOutPut.Rotation => Rotation;
        readonly KineticStatus IKineticOutPut.Status => Status;
    }
}
