namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     An <em>authored</em> trajectory key: its value is owned by a ScriptableObject and changed in the
    ///     inspector, not in code. Implemented by <c>TrajectoryDataBase</c>. It has no members of its own —
    ///     it exists so <see cref="IKineticTrajectoryEditor.Refresh{TTrajectory, TInput}"/> accepts only
    ///     assets, while code keys go through <see cref="IKineticTrajectoryEditor.Update{TTrajectory, TInput}"/>.
    /// </summary>
    public interface ITrajectoryAssetData<TTrajectory, TInput> : ITrajectoryData<TTrajectory, TInput>
        where TInput : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TInput>
    {
    }
}
