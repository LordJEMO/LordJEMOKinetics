namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The implementation side of the manager's Bridge: turns one trajectory into a group backed by a
    ///     specific runtime (single-threaded, Jobs/Burst, …). <see cref="KineticManager"/> depends only on
    ///     this interface, so the backend is chosen by the builder it is given, not by the manager type —
    ///     the trajectory analogue of <c>IBlockGridTemplateBuilder</c>.
    /// </summary>
    public interface IKineticTrajectoryGroupBuilder
    {
        /// <summary>Creates an empty group whose every entry flies <paramref name="trajectory"/>. The value is fixed for the group's lifetime.</summary>
        IKineticTrajectoryGroup<TTrajectory, TInput> Build<TTrajectory, TInput>(in TTrajectory trajectory)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;
    }
}
