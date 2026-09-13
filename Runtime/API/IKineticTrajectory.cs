namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     A motion law — the extension point of the whole system. One implementation per way a body
    ///     can travel (<see cref="LinearTrajectory"/> now; ballistic, homing, spline later). A
    ///     <see cref="KineticAnimator{TTrajectory, TContext}"/> is injected with one and does nothing
    ///     but advance a clock and call <see cref="Evaluate"/>.
    ///
    ///     <see cref="Evaluate"/> — pose plus <see cref="KineticStatus"/> — is the whole base
    ///     contract; it is all an unbounded law (pursuit, orbit) can honour. Timeline semantics
    ///     (<c>Duration</c>, <c>NormalizedTime</c>) live on <see cref="IKineticClip{TContext}"/>.
    ///
    ///     <typeparamref name="TContext"/> is the concrete <see cref="IKineticInput"/> this law is
    ///     written against — a point-to-point law pins <see cref="KineticInput"/>; a spline law
    ///     would pin its own waypoint-list context.
    ///
    ///     Implementations MUST be <c>[Serializable]</c> <c>unmanaged</c> <c>struct</c>s (no classes,
    ///     no managed fields): <c>[Serializable]</c> so <c>[SerializeReference]</c> can offer them in
    ///     the inspector, <c>unmanaged</c> so they satisfy the
    ///     <c>where TTrajectory : unmanaged, IKineticTrajectory&lt;TContext&gt;</c> constraint and
    ///     drop straight into a <see cref="Unity.Collections.NativeArray{T}"/> / <c>IComponentData</c>
    ///     with Burst monomorphising the call (no boxing, no virtual dispatch).
    /// </summary>
    public interface IKineticTrajectory<TKineticInput> 
        where TKineticInput : unmanaged, IKineticInput
    {
        /// <summary>Full pose (position + direction + rotation + status, plus <c>NormalizedTime</c> for a clip) at <paramref name="time"/> seconds, given <paramref name="context"/>.</summary>
        KineticOutput Evaluate(in TKineticInput kineticInput, float time);
    }
}
