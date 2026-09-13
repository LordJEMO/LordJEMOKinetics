using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Non-generic root for every authored trajectory asset. Exists so a heterogeneous list of assets is
    ///     a plain <c>[SerializeField] TrajectoryAssetBase[]</c> — a serialized field of a
    ///     <em>constructed generic</em> <see cref="ScriptableObject"/> type is not reliably supported. Cast to
    ///     the <see cref="ITrajectoryData{TTrajectory, TInput}"/> you need.
    /// </summary>
    public abstract class TrajectoryAssetBase : ScriptableObject
    {
    }

    /// <summary>
    ///     Base for a ScriptableObject that authors one <typeparamref name="TTrajectory"/>. The asset is a
    ///     data-only bridge and also its own registration key: <c>registry.Register(asset)</c> from the
    ///     composition root, <c>registry.GetEmitter(asset)</c> from any component that references the same
    ///     asset. After editing it at runtime, <see cref="IKineticTrajectoryEditor.Refresh{TTrajectory, TInput}"/>
    ///     pushes the new value into the live trajectory — never automatically.
    ///
    ///     A subclass that serializes the trajectory struct directly just returns it; one that serializes
    ///     separate parameters overrides <see cref="Sync"/> to rebuild a cached trajectory from them.
    /// </summary>
    public abstract class TrajectoryDataBase<TTrajectory, TInput> : TrajectoryAssetBase, ITrajectoryAssetData<TTrajectory, TInput>
        where TInput : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TInput>
    {
        /// <summary>The authored rule value. Never calls <see cref="Sync"/>.</summary>
        public abstract TTrajectory Trajectory { get; }

        /// <summary>
        ///     Rebuilds the cached trajectory from authored parameters. Runs on <c>OnEnable</c> (so a player
        ///     build fills a non-serialized cache on load) and on <c>OnValidate</c> (inspector edits).
        /// </summary>
        protected virtual void Sync()
        {
        }

        private void OnEnable()
        {
            Sync();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Sync();
        }
#endif
    }
}
