using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Advances a group's <see cref="KineticEntry{TContext}"/> one frame — every entry driven by
    ///     the same <see cref="Trajectory"/> value. The per-entry work is delegated to a transient
    ///     <see cref="KineticAnimator{TTrajectory, TContext}"/> so the parked-skip + evaluate logic
    ///     stays in one place.
    ///
    ///     Needs a <c>RegisterGenericJobType</c> line per concrete pair — see
    ///     <see cref="KineticJobRegistration"/>.
    /// </summary>
    [BurstCompile]
    public struct KineticEntryAdvanceJob<TTrajectory, TContext> : IJobFor
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        public TTrajectory Trajectory;

        public NativeArray<KineticEntry<TContext>> Entries;

        public float DeltaTime;

        public void Execute(int index)
        {
            KineticEntry<TContext> entry = Entries[index];

            KineticAnimator<TTrajectory, TContext> animator = default;
            animator.Trajectory = Trajectory;
            animator.Context = entry.Context;
            animator.State = entry.State;

            animator.Tick(DeltaTime);   // no-ops while Paused / Completed

            entry.Context = animator.Context;
            entry.State = animator.State;
            Entries[index] = entry;
        }
    }
}
