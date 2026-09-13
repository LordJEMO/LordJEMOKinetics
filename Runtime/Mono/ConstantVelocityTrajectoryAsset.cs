using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Authoring asset for a <see cref="ConstantVelocityTrajectory"/> rule. Its entries use
    ///     <see cref="KineticVelocityInput"/> (a launch point + a velocity), not the origin/target
    ///     <see cref="KineticInput"/> — so its trajectory cannot be fed by a spawner that only produces
    ///     <see cref="KineticInput"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "LordJEMO/Kinematics/Constant Velocity Trajectory", fileName = "ConstantVelocityTrajectory")]
    public sealed class ConstantVelocityTrajectoryAsset : TrajectoryDataBase<ConstantVelocityTrajectory, KineticVelocityInput>
    {
        [SerializeField] private ConstantVelocityTrajectory trajectory = ConstantVelocityTrajectory.Forever();

        public override ConstantVelocityTrajectory Trajectory => trajectory;
    }
}
