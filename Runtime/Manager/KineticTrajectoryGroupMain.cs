namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Single-threaded group backend: a plain <c>for</c> loop advances the entries (each via a
    ///     transient <see cref="KineticAnimator{TTrajectory, TContext}"/> over the group's one
    ///     <see cref="KineticTrajectoryGroupBase{TTrajectory, TContext}.Trajectory"/>) and a
    ///     main-thread loop writes the transforms. Contains no <c>Unity.Jobs</c> / <c>Unity.Burst</c>
    ///     reference, so it stays valid on WebGL and other stripped runtimes.
    /// </summary>
    public sealed class KineticTrajectoryGroupMain<TTrajectory, TContext> : KineticTrajectoryGroupBase<TTrajectory, TContext>
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        public KineticTrajectoryGroupMain(in TTrajectory trajectory, int initialCapacity = 16)
            : base(in trajectory, initialCapacity)
        {
        }

        protected override void Advance(float deltaTime)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                KineticEntry<TContext> entry = entries[i];

                KineticAnimator<TTrajectory, TContext> animator = default;
                animator.Trajectory = Trajectory;
                animator.Context = entry.Context;
                animator.State = entry.State;

                animator.Tick(deltaTime);   // already skips Paused / Completed

                entry.Context = animator.Context;
                entry.State = animator.State;
                entries[i] = entry;
            }
        }

        public override void ApplyTransforms()
        {
            for (int i = 0; i < transforms.Count; i++)
            {
                if (writeTransform[i] == 0)
                {
                    continue;
                }

                UnityEngine.Transform target = transforms[i];
                if (target == null)
                {
                    continue;
                }

                KineticPlayback state = entries[i].State;
                target.SetPositionAndRotation(state.Position, state.Rotation);
            }
        }
    }
}
