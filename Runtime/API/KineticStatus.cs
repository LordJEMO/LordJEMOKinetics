namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Lifecycle state of a moving body. Replaces a plain "done?" bool so a live
    ///     <see cref="KineticPlayback"/> can distinguish "not started", "running", and "held" from
    ///     "finished". <c>byte</c>-backed so it stays cheap inside a
    ///     <see cref="Unity.Collections.NativeArray{T}"/> / <c>IComponentData</c>.
    ///
    ///     <see cref="Idle"/> is <c>0</c>, so <c>default(KineticPlayback)</c> is a not-yet-started body
    ///     rather than an ambiguous "not completed". A pure <see cref="KineticOutput"/> only ever
    ///     reports <see cref="Playing"/> or <see cref="Completed"/> — a trajectory cannot know it has
    ///     been paused; only a driver sets <see cref="Paused"/>.
    /// </summary>
    public enum KineticStatus : byte
    {
        /// <summary>Not started. The value of a freshly zeroed <see cref="KineticPlayback"/>.</summary>
        Idle = 0,

        /// <summary>Advancing every tick.</summary>
        Playing = 1,

        /// <summary>Held — the clock is frozen. The batch job and the Entities system skip these.</summary>
        Paused = 2,

        /// <summary>End reached — temporal for an <see cref="IKineticClip{TContext}"/>, spatial (arrival radius) for a behaviour.</summary>
        Completed = 3,
    }
}
