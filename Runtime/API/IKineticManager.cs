using System;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The frame loop of the kinetic system. <see cref="KineticManager"/> plays four roles, each behind its
    ///     own interface so every component receives only what it needs:
    ///     <list type="number">
    ///       <item><see cref="IKineticManager"/> — the owner ticks and applies transforms, then disposes.</item>
    ///       <item><see cref="IKineticTrajectoryRegistry"/> — the composition root registers trajectories
    ///       (from an asset or a <see cref="KineticTrajectoryKey{TTrajectory, TInput}"/>); other components look
    ///       emitters up with the same key.</item>
    ///       <item><see cref="IKineticTrajectoryEditor"/> — one dedicated helper changes a trajectory.</item>
    ///       <item><see cref="IKineticEmitter{TInput}"/> — gameplay components add moving objects and control them
    ///       through the returned <see cref="KineticObject"/>.</item>
    ///     </list>
    ///     Frame flow: <see cref="Tick"/> → read entry status → <see cref="ApplyTransforms"/>. Transforms are
    ///     externally owned: nothing here instantiates or destroys one.
    /// </summary>
    public interface IKineticManager : IDisposable
    {
        /// <summary>Total live entries across all trajectories.</summary>
        int Count { get; }

        /// <summary>Advance every entry by <paramref name="deltaTime"/>.</summary>
        void Tick(float deltaTime);

        /// <summary>Write the resolved pose onto every opted-in entry's transform.</summary>
        void ApplyTransforms();
    }
}
