using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The <b>simple</b> manager driver — single-threaded, no Jobs/Burst
    ///     (<see cref="KineticTrajectoryGroupMainBuilder"/>). Safe on WebGL and stripped runtimes; use it
    ///     when the body count is modest or the platform can't run the Job System.
    /// </summary>
    [AddComponentMenu("LordJEMO/Kinematics/Kinetic Manager (Main)")]
    public sealed class KineticManagerMainBehaviour : KineticManagerBehaviourBase
    {
        protected override IKineticTrajectoryGroupBuilder CreateGroupBuilder(int capacity)
        {
            return new KineticTrajectoryGroupMainBuilder(capacity);
        }
    }
}
