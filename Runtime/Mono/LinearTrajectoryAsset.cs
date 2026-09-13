using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Authoring asset for a <see cref="LinearTrajectory"/> rule — set the general values here, then
    ///     <c>registry.Register(asset)</c>. One asset type per trajectory kind; copy this file and swap the
    ///     two type arguments for a new law.
    /// </summary>
    [CreateAssetMenu(menuName = "LordJEMO/Kinematics/Linear Trajectory", fileName = "LinearTrajectory")]
    public sealed class LinearTrajectoryAsset : TrajectoryDataBase<LinearTrajectory, KineticInput>
    {
        [SerializeField] private LinearTrajectory trajectory = LinearTrajectory.OverTime(3f);

        public override LinearTrajectory Trajectory => trajectory;
    }
}
