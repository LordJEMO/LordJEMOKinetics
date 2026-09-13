namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Builds single-threaded <see cref="KineticTrajectoryGroupMain{TTrajectory, TContext}"/> groups. No
    ///     Jobs/Burst — safe on WebGL and stripped runtimes.
    /// </summary>
    public sealed class KineticTrajectoryGroupMainBuilder : IKineticTrajectoryGroupBuilder
    {
        private readonly int groupCapacity;

        public KineticTrajectoryGroupMainBuilder(int groupCapacity = 16)
        {
            this.groupCapacity = groupCapacity;
        }

        public IKineticTrajectoryGroup<TTrajectory, TInput> Build<TTrajectory, TInput>(in TTrajectory trajectory)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            return new KineticTrajectoryGroupMain<TTrajectory, TInput>(in trajectory, groupCapacity);
        }
    }
}
