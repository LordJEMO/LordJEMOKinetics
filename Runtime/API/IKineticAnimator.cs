namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Non-generic view of a single-body animator, for managed / editor consumers that hold an
    ///     animator without knowing its trajectory and context type arguments.
    ///     <see cref="KineticAnimator{TTrajectory, TContext}"/> implements it.
    ///
    ///     Using an animator <em>through</em> this interface boxes the struct, so the batched
    ///     Job-System path (<see cref="KineticUpdateJob{TTrajectory, TContext}"/>,
    ///     <see cref="KineticAnimatorAdvanceJob{TTrajectory, TContext}"/>) keeps working with the
    ///     concrete struct directly.
    /// </summary>
    public interface IKineticAnimator
    {
        /// <summary>The pose, clock and status maintained by this animator.</summary>
        KineticPlayback State { get; }

        /// <summary>Snaps back to the trajectory start and enters <see cref="KineticStatus.Playing"/>.</summary>
        void Reset();

        /// <summary>Advances the clock by <paramref name="deltaTime"/> and re-samples. A no-op while paused or completed.</summary>
        void Tick(float deltaTime);

        /// <summary>Freezes the clock.</summary>
        void Pause();

        /// <summary>Un-freezes the clock.</summary>
        void Resume();
    }
}
