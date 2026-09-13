namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     A trajectory key created in code, for rules that have no asset. Share the instance with every
    ///     component that needs the trajectory's emitter. Its value only changes through
    ///     <see cref="IKineticTrajectoryEditor.Update{TTrajectory, TInput}"/>.
    /// </summary>
    public sealed class KineticTrajectoryKey<TTrajectory, TInput> : ITrajectoryData<TTrajectory, TInput>
        where TInput : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TInput>
    {
        public KineticTrajectoryKey(in TTrajectory trajectory)
        {
            Trajectory = trajectory;
        }

        public TTrajectory Trajectory { get; internal set; }
    }
}
