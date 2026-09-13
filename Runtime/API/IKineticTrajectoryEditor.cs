using System.Collections.Generic;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The only way to change a registered trajectory. Give it to one dedicated helper, not to gameplay
    ///     components. A change replaces the value in place: the same group, emitters and objects stay valid,
    ///     and every live entry flies the new rule from the next <see cref="IKineticManager.Tick"/> without
    ///     being rewound.
    /// </summary>
    public interface IKineticTrajectoryEditor
    {
        /// <summary>Pushes an asset's current authored value into its live trajectory (after it was edited).</summary>
        /// <exception cref="KeyNotFoundException"><paramref name="asset"/> is not registered.</exception>
        void Refresh<TTrajectory, TInput>(ITrajectoryAssetData<TTrajectory, TInput> asset)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;

        /// <summary>Stores <paramref name="trajectory"/> on the code key and applies it to its live trajectory.</summary>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not registered; the key is left unchanged.</exception>
        void Update<TTrajectory, TInput>(KineticTrajectoryKey<TTrajectory, TInput> key, in TTrajectory trajectory)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;
    }
}
