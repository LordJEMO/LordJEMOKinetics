using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The <b>parallelizable</b> manager driver — Burst <c>IJobFor</c> advance +
    ///     <c>IJobParallelForTransform</c> transform write (<see cref="KineticTrajectoryGroupJobsBuilder"/>).
    ///     Use it for large body counts on platforms with the Job System.
    /// </summary>
    [AddComponentMenu("LordJEMO/Kinematics/Kinetic Manager (Jobs)")]
    public sealed class KineticManagerJobsBehaviour : KineticManagerBehaviourBase
    {
        protected override IKineticTrajectoryGroupBuilder CreateGroupBuilder(int capacity)
        {
            return new KineticTrajectoryGroupJobsBuilder(capacity);
        }
    }
}
