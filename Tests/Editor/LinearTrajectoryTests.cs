using LordJEMO.Kinematics;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace LordJEMO.Tests.EditMode.Kinematics
{
    [TestFixture, Category("Kinematics")]
    public class LinearTrajectoryTests
    {
        private const float Tolerance = 1e-4f;

        private static KineticInput Fixture()
        {
            return new KineticInput(new float3(1f, 2f, -3f), new float3(11f, 2f, 5f));
        }

        [Test]
        public void LinearTrajectory_IsAClip()
        {
            Assert.IsInstanceOf<IKineticClip<KineticInput>>(new LinearTrajectory());
        }

        [Test]
        public void DefaultState_IsIdle()
        {
            KineticPlayback state = default;
            Assert.AreEqual(KineticStatus.Idle, state.Status);
        }

        [Test]
        public void Linear_AtStart_IsOriginAndPlaying()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            KineticOutput sample = trajectory.Evaluate(in context, 0f);

            Assert.That(math.distance(sample.Position, context.Origin), Is.LessThan(Tolerance), "Position at t=0.");
            Assert.AreEqual(KineticStatus.Playing, sample.Status, "Playing at t=0.");
        }

        [Test]
        public void Linear_PastDuration_ClampsToTargetAndCompletes()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            KineticOutput sample = trajectory.Evaluate(in context, 999f);

            Assert.That(math.distance(sample.Position, context.Target), Is.LessThan(Tolerance), "Position clamps to Target.");
            Assert.That(sample.NormalizedTime, Is.EqualTo(1f).Within(Tolerance), "NormalizedTime clamps to 1.");
            Assert.AreEqual(KineticStatus.Completed, sample.Status, "Completed past duration.");
        }

        [Test]
        public void Linear_NormalizedTime_HalfwayIsHalf()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            Assert.That(trajectory.NormalizedTime(in context, 2f), Is.EqualTo(0.5f).Within(Tolerance), "Half duration -> 0.5.");
        }

        [Test]
        public void Linear_Midpoint_IsHalfway()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            float3 position = trajectory.Evaluate(in context, 2f).Position;
            float3 expected = math.lerp(context.Origin, context.Target, 0.5f);

            Assert.That(math.distance(position, expected), Is.LessThan(Tolerance), "Halfway position.");
        }

        [Test]
        public void Linear_Rotation_FacesTravelDirection()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            KineticOutput sample = trajectory.Evaluate(in context, 1f);

            float3 forward = math.mul(sample.Rotation, math.forward());
            float3 expected = math.normalize(context.Target - context.Origin);

            Assert.That(math.dot(forward, expected), Is.EqualTo(1f).Within(Tolerance), "Rotation forward aligns with Origin->Target.");
        }

        [Test]
        public void Duration_FromSpeed_IsDistanceOverSpeed()
        {
            LinearTrajectory trajectory = LinearTrajectory.AtSpeed(10f);
            KineticInput context = new KineticInput(float3.zero, new float3(0f, 0f, 20f));

            float duration = trajectory.Duration(in context);

            Assert.That(duration, Is.EqualTo(2f).Within(Tolerance), "20 units / 10 ups = 2 s.");
        }

        [Test]
        public void Sample_ReadThrough_IKineticPose()
        {
            LinearTrajectory trajectory = LinearTrajectory.OverTime(4f);
            KineticInput context = Fixture();

            KineticOutput sample = trajectory.Evaluate(in context, 999f);
            IKineticOutPut pose = sample;

            Assert.That(math.distance(pose.Position, sample.Position), Is.LessThan(Tolerance), "Position via interface.");
            Assert.AreEqual(sample.Status, pose.Status, "Status via interface.");
        }

        [Test]
        public void Animator_SingleBigStep_MatchesManySmallSteps()
        {
            KineticAnimator<LinearTrajectory, KineticInput> coarse = default;
            coarse.Trajectory = LinearTrajectory.OverTime(4f);
            coarse.Context = Fixture();
            coarse.Reset();
            coarse.Tick(1.6f);

            KineticAnimator<LinearTrajectory, KineticInput> fine = default;
            fine.Trajectory = LinearTrajectory.OverTime(4f);
            fine.Context = Fixture();
            fine.Reset();
            for (int step = 0; step < 16; step++)
            {
                fine.Tick(0.1f);
            }

            Assert.That(math.distance(coarse.State.Position, fine.State.Position), Is.LessThan(Tolerance), "Linear motion is step-size independent.");
        }

        [Test]
        public void Animator_Paused_DoesNotAdvance()
        {
            KineticAnimator<LinearTrajectory, KineticInput> animator = default;
            animator.Trajectory = LinearTrajectory.OverTime(4f);
            animator.Context = Fixture();
            animator.Reset();
            animator.Tick(1f);

            float3 held = animator.State.Position;
            float heldTime = animator.State.Time;

            animator.Pause();
            animator.Tick(1f);

            Assert.AreEqual(KineticStatus.Paused, animator.State.Status);
            Assert.That(math.distance(animator.State.Position, held), Is.LessThan(Tolerance), "Position frozen while paused.");
            Assert.That(animator.State.Time, Is.EqualTo(heldTime).Within(Tolerance), "Clock frozen while paused.");

            animator.Resume();
            animator.Tick(1f);
            Assert.That(animator.State.Time, Is.GreaterThan(heldTime), "Clock advances after resume.");
        }

        [Test]
        public void UpdateJob_AgreesWith_DirectEvaluate_AndSkipsParked()
        {
            const int count = 256;
            const float deltaTime = 0.05f;
            const int steps = 30;
            const int pausedIndex = 7;

            NativeArray<LinearTrajectory> trajectories = new NativeArray<LinearTrajectory>(count, Allocator.TempJob);
            NativeArray<KineticInput> contexts = new NativeArray<KineticInput>(count, Allocator.TempJob);
            NativeArray<KineticPlayback> states = new NativeArray<KineticPlayback>(count, Allocator.TempJob);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    float3 origin = new float3(i, 0f, 0f);
                    float3 target = new float3(i, 10f + i, 0f);

                    trajectories[i] = LinearTrajectory.OverTime(1f + (i % 5));
                    contexts[i] = new KineticInput(origin, target);

                    KineticPlayback state = default;
                    state.Position = origin;
                    state.Rotation = quaternion.identity;
                    state.Status = i == pausedIndex ? KineticStatus.Paused : KineticStatus.Playing;
                    states[i] = state;
                }

                for (int step = 0; step < steps; step++)
                {
                    KineticBatch.Run(trajectories, contexts, states, deltaTime);
                }

                float totalTime = deltaTime * steps;
                for (int i = 0; i < count; i++)
                {
                    if (i == pausedIndex)
                    {
                        Assert.AreEqual(KineticStatus.Paused, states[i].Status, "Paused body kept its status.");
                        Assert.That(states[i].Time, Is.EqualTo(0f).Within(Tolerance), "Paused body clock never advanced.");
                        continue;
                    }

                    KineticInput context = contexts[i];
                    KineticOutput expected = trajectories[i].Evaluate(in context, totalTime);

                    Assert.That(math.distance(states[i].Position, expected.Position), Is.LessThan(Tolerance), $"Job vs direct position mismatch at {i}.");
                    Assert.That(math.abs(math.dot(states[i].Rotation.value, expected.Rotation.value)), Is.EqualTo(1f).Within(Tolerance), $"Job vs direct rotation mismatch at {i}.");
                }
            }
            finally
            {
                if (trajectories.IsCreated)
                {
                    trajectories.Dispose();
                }

                if (contexts.IsCreated)
                {
                    contexts.Dispose();
                }

                if (states.IsCreated)
                {
                    states.Dispose();
                }
            }
        }
    }
}
