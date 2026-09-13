using Unity.Collections;
using Unity.Jobs;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Thin scheduling helpers around <see cref="KineticUpdateJob{TTrajectory, TContext}"/> for
    ///     callers that keep a pool of bodies as parallel <see cref="NativeArray{T}"/>s (a bullet
    ///     manager, a swarm — see <see cref="KineticBatchSample"/>). Uses
    ///     <see cref="IJobForExtensions.ScheduleParallelByRef"/> so the job struct is not copied by
    ///     value at the schedule call. Mirrors <see cref="LordJEMO.Utilities.Jobs.NumericalSeries"/>.
    ///
    ///     Allocator guidance for the arrays the caller owns:
    ///     <list type="bullet">
    ///       <item><c>Allocator.Persistent</c> — a long-lived pool reused across many frames.</item>
    ///       <item><c>Allocator.TempJob</c> — a one-shot volley scheduled and disposed within 4 frames.</item>
    ///     </list>
    ///     This class allocates nothing; the caller disposes what the caller created.
    /// </summary>
    public static class KineticBatch
    {
        /// <summary>
        ///     Schedules one parallel update pass over bodies that share the motion law
        ///     <typeparamref name="TTrajectory"/> and context type <typeparamref name="TContext"/>.
        ///     Does not <see cref="JobHandle.Complete"/> — the caller chains the returned handle.
        /// </summary>
        /// <param name="trajectories">Per-body motion laws; not modified.</param>
        /// <param name="contexts">Per-body data; not modified. Same length as <paramref name="trajectories"/>.</param>
        /// <param name="states">Per-body runtime; advanced in place. Same length as <paramref name="trajectories"/>.</param>
        /// <param name="deltaTime">Seconds to advance every body's clock.</param>
        /// <param name="innerloopBatchCount">Work items per parallel batch; 64 suits cheap per-item work.</param>
        /// <param name="dependency">Optional upstream handle to chain after.</param>
        public static JobHandle Schedule<TTrajectory, TContext>(
            NativeArray<TTrajectory> trajectories,
            NativeArray<TContext> contexts,
            NativeArray<KineticPlayback> states,
            float deltaTime,
            int innerloopBatchCount = 64,
            JobHandle dependency = default)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TContext>
        {
            KineticUpdateJob<TTrajectory, TContext> job = new KineticUpdateJob<TTrajectory, TContext>
            {
                Trajectories = trajectories,
                Contexts = contexts,
                States = states,
                DeltaTime = deltaTime,
            };

            return IJobForExtensions.ScheduleParallelByRef(ref job, states.Length, innerloopBatchCount, dependency);
        }

        /// <summary>Schedules a pass and blocks until it finishes. Convenience for synchronous call sites and tests.</summary>
        public static void Run<TTrajectory, TContext>(
            NativeArray<TTrajectory> trajectories,
            NativeArray<TContext> contexts,
            NativeArray<KineticPlayback> states,
            float deltaTime,
            int innerloopBatchCount = 64)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TContext>
        {
            Schedule(trajectories, contexts, states, deltaTime, innerloopBatchCount).Complete();
        }

        /// <summary>
        ///     Group variant: one pass over a dense array of <see cref="KineticEntry{TContext}"/>, all
        ///     driven by the single <paramref name="trajectory"/> value. Used by
        ///     <c>KineticTrajectoryGroupJobs</c>. Does not <see cref="JobHandle.Complete"/>.
        /// </summary>
        public static JobHandle Schedule<TTrajectory, TContext>(
            TTrajectory trajectory,
            NativeArray<KineticEntry<TContext>> entries,
            float deltaTime,
            int innerloopBatchCount = 64,
            JobHandle dependency = default)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TContext>
        {
            KineticEntryAdvanceJob<TTrajectory, TContext> job = new KineticEntryAdvanceJob<TTrajectory, TContext>
            {
                Trajectory = trajectory,
                Entries = entries,
                DeltaTime = deltaTime,
            };

            return IJobForExtensions.ScheduleParallelByRef(ref job, entries.Length, innerloopBatchCount, dependency);
        }

        /// <summary>Group variant of <see cref="Run{TTrajectory, TContext}(NativeArray{TTrajectory}, NativeArray{TContext}, NativeArray{KineticPlayback}, float, int)"/> — schedules and blocks.</summary>
        public static void Run<TTrajectory, TContext>(
            TTrajectory trajectory,
            NativeArray<KineticEntry<TContext>> entries,
            float deltaTime,
            int innerloopBatchCount = 64)
            where TContext : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TContext>
        {
            Schedule(trajectory, entries, deltaTime, innerloopBatchCount).Complete();
        }
    }
}
