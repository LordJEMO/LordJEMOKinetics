namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Builds Burst / Jobs <see cref="KineticTrajectoryGroupJobs{TTrajectory, TContext}"/> groups. Every
    ///     trajectory/input pair used here needs its lines in <c>Jobs/KineticJobRegistration.cs</c>.
    /// </summary>
    public sealed class KineticTrajectoryGroupJobsBuilder : IKineticTrajectoryGroupBuilder
    {
        private readonly int groupCapacity;

        public KineticTrajectoryGroupJobsBuilder(int groupCapacity = 64)
        {
            this.groupCapacity = groupCapacity;
        }

        public IKineticTrajectoryGroup<TTrajectory, TInput> Build<TTrajectory, TInput>(in TTrajectory trajectory)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            return new KineticTrajectoryGroupJobs<TTrajectory, TInput>(in trajectory, groupCapacity);
        }
    }
}
