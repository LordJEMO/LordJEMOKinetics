namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     A <em>bounded</em>, seekable motion law — one with a timeline. Adds to the base
    ///     <see cref="IKineticTrajectory{TContext}"/> the two things that only make sense when a total
    ///     duration is knowable up front: <see cref="Duration"/> and <see cref="NormalizedTime"/>.
    ///
    ///     <see cref="LinearTrajectory"/> is a clip; a ballistic-to-target arc and an arc-length
    ///     spline would be too. A target-following / pursuit law is NOT — its target moves, so it has
    ///     no total duration; it implements only the base interface and signals arrival through
    ///     <see cref="KineticStatus.Completed"/> alone. Code that scrubs progress (VFX curves, a
    ///     timeline) constrains to <see cref="IKineticClip{TContext}"/> so the compiler rejects the
    ///     unbounded laws.
    /// </summary>
    public interface IKineticClip<TContext> : IKineticTrajectory<TContext>
        where TContext : unmanaged, IKineticInput
    {
        /// <summary>Total travel time in seconds. <c>0</c> means degenerate (snaps to the end on the first evaluation).</summary>
        float Duration(in TContext context);

        /// <summary>
        ///     <paramref name="time"/> mapped to <c>[0, 1]</c> over <see cref="Duration"/>. A clip's
        ///     <see cref="IKineticTrajectory{TContext}.Evaluate"/> also puts this in
        ///     <see cref="KineticOutput.NormalizedTime"/>; a non-clip law leaves that field <c>0</c>.
        /// </summary>
        float NormalizedTime(in TContext context, float time);
    }
}
