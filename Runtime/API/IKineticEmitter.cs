using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Adds moving objects (bullets, units) to one registered trajectory. This is all a gameplay component
    ///     needs: it knows neither the manager, the group nor the trajectory type — only the input it
    ///     authors, checked at compile time. Obtained from <see cref="IKineticTrajectoryRegistry"/>.
    /// </summary>
    public interface IKineticEmitter<TInput>
        where TInput : unmanaged, IKineticInput
    {
        /// <summary>
        ///     Adds an entry. <paramref name="transform"/> may be <c>null</c> (headless);
        ///     <paramref name="writeTransform"/> <c>false</c> keeps the transform but leaves it untouched by
        ///     <see cref="IKineticManager.ApplyTransforms"/>. The returned object controls and removes the entry.
        /// </summary>
        KineticObject Add(in TInput input, Transform transform = null, bool writeTransform = true);
    }
}
