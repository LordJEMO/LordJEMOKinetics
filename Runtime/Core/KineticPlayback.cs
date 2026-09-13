using System;
using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The per-body mutable runtime of a motion: the elapsed clock plus the last sampled pose and
    ///     lifecycle status. This is the "animator state" half of the system, and the source of truth
    ///     for the batched Job-System path — a <see cref="Unity.Collections.NativeArray{T}"/> of these
    ///     is what <see cref="KineticUpdateJob{TTrajectory, TContext}"/> fills. It is advanced by
    ///     <see cref="KineticAnimator{TTrajectory, TContext}"/> (single body) or written directly by
    ///     the job (batched / Burst); either way the values come from an
    ///     <see cref="IKineticTrajectory{TContext}"/>.
    ///
    ///     Blittable so it satisfies <c>unmanaged</c>. NOTE: <c>default(KineticPlayback)</c> is
    ///     <see cref="KineticStatus.Idle"/> with an invalid <c>(0,0,0,0)</c> <see cref="Rotation"/> —
    ///     call <see cref="KineticAnimator{TTrajectory, TContext}.Reset"/> (or seed
    ///     <c>quaternion.identity</c> and <see cref="KineticStatus.Playing"/>) before reading it.
    /// </summary>
    [Serializable]
    public struct KineticPlayback : IKineticOutPut
    {
        /// <summary>Seconds elapsed since the motion was reset.</summary>
        public float Time;

        /// <summary>World-space position sampled at <see cref="Time"/>.</summary>
        public float3 Position;

        /// <summary>Unit travel direction sampled at <see cref="Time"/>; <c>float3.zero</c> when undefined.</summary>
        public float3 Direction;

        /// <summary>Orientation sampled at <see cref="Time"/>, decided by the trajectory. See <see cref="KineticOutput.Rotation"/>.</summary>
        public quaternion Rotation;

        /// <summary>Lifecycle status. Set by the driver; <see cref="KineticStatus.Idle"/> until first played.</summary>
        public KineticStatus Status;

        readonly float3 IKineticOutPut.Position => Position;
        readonly float3 IKineticOutPut.Direction => Direction;
        readonly quaternion IKineticOutPut.Rotation => Rotation;
        readonly KineticStatus IKineticOutPut.Status => Status;
    }
}
