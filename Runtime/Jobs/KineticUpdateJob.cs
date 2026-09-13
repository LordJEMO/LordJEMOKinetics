using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Advances a whole batch of bodies one frame, all sharing the motion law
    ///     <typeparamref name="TTrajectory"/> and context type <typeparamref name="TContext"/>.
    ///     Iteration <c>i</c> reads <c>Trajectories[i]</c> + <c>Contexts[i]</c>, advances
    ///     <c>States[i].Time</c> by <see cref="DeltaTime"/>, and writes the freshly sampled pose back
    ///     into <c>States[i]</c> — the exact work
    ///     <see cref="KineticAnimator{TTrajectory, TContext}.Tick"/> does for a single body.
    ///
    ///     An <see cref="IJobFor"/> (not <see cref="IJobParallelFor"/>) so callers can pick the
    ///     schedule mode — <see cref="IJobForExtensions.ScheduleParallelByRef"/> for the batched case
    ///     (see <see cref="KineticBatch"/>), <c>ScheduleByRef</c> for a strict-order single thread.
    ///     All three arrays are indexed 1:1 and must be the same length. Generic Burst jobs need a
    ///     <c>RegisterGenericJobType</c> line per concrete pair — see <see cref="KineticJobRegistration"/>.
    /// </summary>
    [BurstCompile]
    public struct KineticUpdateJob<TTrajectory, TContext> : IJobFor
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        [ReadOnly] public NativeArray<TTrajectory> Trajectories;
        [ReadOnly] public NativeArray<TContext> Contexts;

        public NativeArray<KineticPlayback> States;

        public float DeltaTime;

        public void Execute(int index)
        {
            // Read-modify-write: the NativeArray indexer returns a copy, so mutating
            // States[index].Field in place would write to a temporary. Pull out, update, assign back.
            KineticPlayback state = States[index];

            // Parked bodies cost nothing: a paused body is held, a completed one is clamped at its
            // end. Replaying means setting Status = Playing (and Time = 0) from outside the job.
            if (state.Status == KineticStatus.Paused || state.Status == KineticStatus.Completed)
            {
                return;
            }

            state.Time += DeltaTime;

            // Copy to locals: a NativeArray indexer result is not an lvalue and cannot bind to `in`.
            TTrajectory trajectory = Trajectories[index];
            TContext context = Contexts[index];

            KineticOutput sample = trajectory.Evaluate(in context, state.Time);

            state.Position = sample.Position;
            state.Direction = sample.Direction;
            state.Rotation = sample.Rotation;
            state.Status = sample.Status;

            States[index] = state;
        }
    }
}
