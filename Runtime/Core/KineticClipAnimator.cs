using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Clip-only affordances for <see cref="KineticAnimator{TTrajectory, TContext}"/>. These are
    ///     extension methods rather than members so they can carry the tighter
    ///     <c>where TTrajectory : unmanaged, IKineticClip&lt;TContext&gt;</c> constraint — the
    ///     compiler rejects them for unbounded laws that have no timeline — without widening the base
    ///     animator. Each call reaches into a concrete <c>unmanaged</c> struct type parameter, so the
    ///     <see cref="IKineticClip{TContext}.Duration"/> call is constrained (no boxing) and Burst-safe.
    /// </summary>
    public static class KineticClipAnimator
    {
        /// <summary>Jumps straight to the end pose: clock at full duration, status <see cref="KineticStatus.Completed"/>.</summary>
        public static void Final<TTrajectory, TContext>(ref this KineticAnimator<TTrajectory, TContext> animator)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticClip<TContext>
        {
            animator.State.Time = animator.Trajectory.Duration(in animator.Context);

            KineticOutput sample = animator.Trajectory.Evaluate(in animator.Context, animator.State.Time);

            animator.State.Position = sample.Position;
            animator.State.Direction = sample.Direction;
            animator.State.Rotation = sample.Rotation;
            animator.State.Status = KineticStatus.Completed;
        }

        /// <summary>Scrubs to a <c>[0, 1]</c> point on the timeline (<c>time = saturate(t01) * Duration</c>) and re-samples.</summary>
        public static void SeekNormalized<TTrajectory, TContext>(ref this KineticAnimator<TTrajectory, TContext> animator, float t01)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticClip<TContext>
        {
            float duration = animator.Trajectory.Duration(in animator.Context);
            animator.State.Time = math.saturate(t01) * duration;

            KineticOutput sample = animator.Trajectory.Evaluate(in animator.Context, animator.State.Time);

            animator.State.Position = sample.Position;
            animator.State.Direction = sample.Direction;
            animator.State.Rotation = sample.Rotation;
            animator.State.Status = sample.Status;
        }
    }
}
