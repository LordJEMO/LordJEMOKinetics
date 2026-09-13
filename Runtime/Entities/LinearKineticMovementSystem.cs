#if KINEMATICS_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Advances every entity carrying <see cref="LinearTrajectoryComponent"/> +
    ///     <see cref="KineticInputComponent"/> + <see cref="KineticPlaybackComponent"/> +
    ///     <see cref="LocalTransform"/> one frame, writing the sampled pose onto
    ///     <see cref="LocalTransform"/>. Same per-body work as
    ///     <see cref="KineticAnimator{TTrajectory, TContext}.Tick"/> /
    ///     <see cref="KineticUpdateJob{TTrajectory, TContext}"/>.
    ///
    ///     One concrete system per trajectory kind — <c>ISystem</c> / <c>IJobEntity</c> do not take
    ///     generic parameters cleanly. Copy this file per new
    ///     <see cref="IKineticTrajectory{TContext}"/> implementation, swapping the trajectory
    ///     component type.
    /// </summary>
    [BurstCompile]
    public partial struct LinearKineticMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LinearKineticAdvanceJob job = new LinearKineticAdvanceJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct LinearKineticAdvanceJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(
                in LinearTrajectoryComponent trajectory,
                in KineticInputComponent context,
                ref KineticPlaybackComponent runtime,
                ref LocalTransform localTransform)
            {
                // Parked bodies cost nothing (mirrors KineticUpdateJob).
                if (runtime.Value.Status is KineticStatus.Paused or KineticStatus.Completed)
                {
                    return;
                }

                runtime.Value.Time += DeltaTime;

                KineticInput ctx = context.Value;
                KineticOutput sample = trajectory.Value.Evaluate(in ctx, runtime.Value.Time);

                runtime.Value.Position = sample.Position;
                runtime.Value.Direction = sample.Direction;
                runtime.Value.Rotation = sample.Rotation;
                runtime.Value.Status = sample.Status;

                localTransform.Position = sample.Position;
                localTransform.Rotation = sample.Rotation;
            }
        }
    }
}
#endif
