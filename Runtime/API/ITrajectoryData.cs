namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     A registrable trajectory: the rule value plus an identity. The object itself is the key an
    ///     <see cref="IKineticTrajectoryRegistry"/> files the trajectory under — compared by reference, never
    ///     by value — so separate components reach the same trajectory by sharing the same object.
    ///     <see cref="Trajectory"/> is read on registration and on a controlled update.
    ///
    ///     Two kinds exist: <see cref="ITrajectoryAssetData{TTrajectory, TInput}"/> (authored in a
    ///     ScriptableObject) and <see cref="KineticTrajectoryKey{TTrajectory, TInput}"/> (created in code).
    /// </summary>
    public interface ITrajectoryData<TTrajectory, TInput>
        where TInput : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TInput>
    {
        /// <summary>The rule value.</summary>
        TTrajectory Trajectory { get; }
    }
}
