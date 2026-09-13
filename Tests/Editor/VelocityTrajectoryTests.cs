using System.Collections.Generic;
using LordJEMO.Kinematics;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Tests.EditMode.Kinematics
{
    [TestFixture, Category("Kinematics")]
    public class VelocityTrajectoryTests
    {
        private const float Tolerance = 1e-4f;

        private static KineticVelocityInput Ctx(float3 origin, float3 velocity)
        {
            return new KineticVelocityInput(origin, velocity);
        }

        private static KineticManager NewMainManager(int capacity = 16)
        {
            return new KineticManager(new KineticTrajectoryGroupMainBuilder(capacity));
        }

        private static KineticTrajectoryKey<ConstantVelocityTrajectory, KineticVelocityInput> Key(ConstantVelocityTrajectory trajectory)
        {
            return new KineticTrajectoryKey<ConstantVelocityTrajectory, KineticVelocityInput>(trajectory);
        }

        /// <summary>Reaches the interface setter the way the generic group code does — a constrained call, no boxing.</summary>
        private static void SetTargetVia<TContext>(ref TContext context, float3 target)
            where TContext : unmanaged, IKineticInput
        {
            context.Target = target;
        }

        private static float3 ReadTargetVia<TContext>(in TContext context)
            where TContext : unmanaged, IKineticInput
        {
            return context.Target;
        }

        #region context

        [Test]
        public void VelocityContext_Target_IsThePointOneSecondAhead()
        {
            KineticVelocityInput context = Ctx(new float3(1f, 2f, 3f), new float3(4f, 0f, 0f));

            Assert.That(math.distance(ReadTargetVia(in context), new float3(5f, 2f, 3f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void VelocityContext_SetTarget_RederivesVelocity()
        {
            KineticVelocityInput context = Ctx(new float3(1f, 0f, 0f), float3.zero);

            SetTargetVia(ref context, new float3(4f, 0f, 0f));

            Assert.That(math.distance(context.Velocity, new float3(3f, 0f, 0f)), Is.LessThan(Tolerance), "Reach the aim point in one second.");
            Assert.That(context.Speed, Is.EqualTo(3f).Within(Tolerance));
        }

        #endregion

        #region trajectory

        [Test]
        public void Forever_TravelsWithoutEverCompleting()
        {
            ConstantVelocityTrajectory rule = ConstantVelocityTrajectory.Forever();
            KineticVelocityInput context = Ctx(float3.zero, new float3(0f, 0f, 5f));

            Assert.That(rule.Duration(in context), Is.EqualTo(float.PositiveInfinity));

            KineticOutput two = rule.Evaluate(in context, 2f);
            Assert.That(math.distance(two.Position, new float3(0f, 0f, 10f)), Is.LessThan(Tolerance));
            Assert.AreEqual(KineticStatus.Playing, two.Status);
            Assert.That(two.NormalizedTime, Is.EqualTo(0f).Within(Tolerance), "Unbounded laws never report progress.");

            KineticOutput late = rule.Evaluate(in context, 1000f);
            Assert.That(math.distance(late.Position, new float3(0f, 0f, 5000f)), Is.LessThan(1e-2f));
            Assert.AreEqual(KineticStatus.Playing, late.Status);
        }

        [Test]
        public void ForSeconds_ClampsAndCompletes()
        {
            ConstantVelocityTrajectory rule = ConstantVelocityTrajectory.ForSeconds(2f);
            KineticVelocityInput context = Ctx(float3.zero, new float3(3f, 0f, 0f));

            Assert.That(rule.Duration(in context), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(rule.NormalizedTime(in context, 1f), Is.EqualTo(0.5f).Within(Tolerance));

            KineticOutput mid = rule.Evaluate(in context, 1f);
            Assert.That(math.distance(mid.Position, new float3(3f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.AreEqual(KineticStatus.Playing, mid.Status);

            KineticOutput past = rule.Evaluate(in context, 99f);
            Assert.That(math.distance(past.Position, new float3(6f, 0f, 0f)), Is.LessThan(Tolerance), "Clamped at the lifetime.");
            Assert.AreEqual(KineticStatus.Completed, past.Status);
            Assert.That(past.NormalizedTime, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Rotation_FacesVelocity()
        {
            ConstantVelocityTrajectory rule = ConstantVelocityTrajectory.Forever();
            KineticVelocityInput context = Ctx(float3.zero, new float3(0f, 0f, 7f));

            KineticOutput sample = rule.Evaluate(in context, 1f);
            float3 forward = math.mul(sample.Rotation, math.forward());

            Assert.That(math.dot(forward, new float3(0f, 0f, 1f)), Is.EqualTo(1f).Within(Tolerance));
        }

        #endregion

        #region groups

        [Test]
        public void VelocityGroup_MainAndJobs_AgreeWithDirectEvaluate()
        {
            const int count = 32;
            const float dt = 0.05f;
            const int steps = 20;

            ConstantVelocityTrajectory rule = ConstantVelocityTrajectory.Forever();

            using KineticManager main = NewMainManager(count);
            using KineticManager jobs = new KineticManager(new KineticTrajectoryGroupJobsBuilder(count));

            IKineticEmitter<KineticVelocityInput> mainEmitter = main.Register(Key(rule));
            IKineticEmitter<KineticVelocityInput> jobEmitter = jobs.Register(Key(rule));

            KineticObject[] mainObjects = new KineticObject[count];
            KineticObject[] jobObjects = new KineticObject[count];
            KineticVelocityInput[] ctxs = new KineticVelocityInput[count];

            for (int i = 0; i < count; i++)
            {
                ctxs[i] = Ctx(new float3(i, 0f, 0f), new float3(0f, 1f + i, 0f));
                mainObjects[i] = mainEmitter.Add(ctxs[i]);
                jobObjects[i] = jobEmitter.Add(ctxs[i]);
            }

            for (int s = 0; s < steps; s++)
            {
                main.Tick(dt);
                jobs.Tick(dt);
            }

            float total = dt * steps;
            for (int i = 0; i < count; i++)
            {
                float3 expected = rule.Evaluate(in ctxs[i], total).Position;

                Assert.That(math.distance(mainObjects[i].State.Position, expected), Is.LessThan(Tolerance), $"Main vs direct at {i}.");
                Assert.That(math.distance(jobObjects[i].State.Position, expected), Is.LessThan(Tolerance), $"Jobs vs direct at {i}.");
            }
        }

        [Test]
        public void TwoContextTypes_CoexistInOneManager()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(new KineticTrajectoryKey<LinearTrajectory, KineticInput>(LinearTrajectory.OverTime(2f)));
            IKineticEmitter<KineticVelocityInput> velocity = manager.Register(Key(ConstantVelocityTrajectory.Forever()));

            KineticObject a = linear.Add(new KineticInput(float3.zero, new float3(4f, 0f, 0f)));
            KineticObject b = velocity.Add(Ctx(float3.zero, new float3(0f, 6f, 0f)));

            Assert.AreEqual(2, manager.TrajectoryCount);
            Assert.AreEqual(2, manager.Count);

            manager.Tick(1f);

            Assert.That(a.State.Position.x, Is.EqualTo(2f).Within(Tolerance), "Linear entry at 50 percent.");
            Assert.That(b.State.Position.y, Is.EqualTo(6f).Within(Tolerance), "Velocity entry one second of travel.");
        }

        [Test]
        public void GetSetContext_RoundTripsThroughTheGroup()
        {
            using KineticManager manager = NewMainManager();

            KineticObject body = manager.Register(Key(ConstantVelocityTrajectory.Forever())).Add(Ctx(float3.zero, new float3(1f, 0f, 0f)));

            body.SetInput(Ctx(new float3(9f, 0f, 0f), new float3(0f, 2f, 0f)));
            KineticVelocityInput read = body.GetInput<KineticVelocityInput>();

            Assert.That(math.distance(read.Origin, new float3(9f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(math.distance(read.Velocity, new float3(0f, 2f, 0f)), Is.LessThan(Tolerance));

            manager.Tick(1f);
            Assert.That(math.distance(body.State.Position, new float3(9f, 2f, 0f)), Is.LessThan(Tolerance), "Advances from the replaced input.");
        }

        #endregion

        #region assets

        [Test]
        public void LinearTrajectoryAsset_CreatesAGroupWithItsValue()
        {
            LinearTrajectoryAsset asset = ScriptableObject.CreateInstance<LinearTrajectoryAsset>();
            try
            {
                using KineticManager manager = NewMainManager();

                IKineticEmitter<KineticInput> emitter = manager.Register(asset);

                Assert.AreEqual(1, manager.TrajectoryCount);
                Assert.AreSame(emitter, manager.GetEmitter(asset), "The asset is its own key.");

                KineticObject body = emitter.Add(new KineticInput(float3.zero, new float3(6f, 0f, 0f)));
                manager.Tick(asset.Trajectory.DurationSeconds * 0.5f);
                Assert.That(body.State.Position.x, Is.EqualTo(3f).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ConstantVelocityTrajectoryAsset_CreatesAVelocityGroup()
        {
            ConstantVelocityTrajectoryAsset asset = ScriptableObject.CreateInstance<ConstantVelocityTrajectoryAsset>();
            try
            {
                using KineticManager manager = NewMainManager();

                IKineticEmitter<KineticVelocityInput> emitter = manager.Register(asset);
                KineticObject body = emitter.Add(Ctx(float3.zero, new float3(0f, 0f, 4f)));

                manager.Tick(1f);

                Assert.AreEqual(1, manager.TrajectoryCount);
                Assert.AreSame(emitter, manager.GetEmitter(asset), "The asset is its own key.");
                Assert.That(math.distance(body.State.Position, new float3(0f, 0f, 4f)), Is.LessThan(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void AssetBases_ShareTheNonGenericRoot()
        {
            LinearTrajectoryAsset linear = ScriptableObject.CreateInstance<LinearTrajectoryAsset>();
            ConstantVelocityTrajectoryAsset velocity = ScriptableObject.CreateInstance<ConstantVelocityTrajectoryAsset>();
            try
            {
                // What the demo harness serializes: a heterogeneous array of the non-generic root.
                TrajectoryAssetBase[] assets = { linear, velocity };

                Assert.IsInstanceOf<ITrajectoryData<LinearTrajectory, KineticInput>>(assets[0]);
                Assert.IsNotInstanceOf<ITrajectoryData<LinearTrajectory, KineticInput>>(assets[1], "A velocity asset is not a linear trajectory.");
                Assert.IsInstanceOf<ITrajectoryData<ConstantVelocityTrajectory, KineticVelocityInput>>(assets[1]);

                // The two key kinds are distinct types.
                Assert.IsInstanceOf<ITrajectoryAssetData<LinearTrajectory, KineticInput>>(assets[0]);
                Assert.IsNotInstanceOf<ITrajectoryAssetData<LinearTrajectory, KineticInput>>(
                    new KineticTrajectoryKey<LinearTrajectory, KineticInput>(LinearTrajectory.OverTime(1f)));
            }
            finally
            {
                Object.DestroyImmediate(linear);
                Object.DestroyImmediate(velocity);
            }
        }

        [Test]
        public void Refresh_EditedAsset_RetunesLiveEntries()
        {
            LinearTrajectoryAsset asset = ScriptableObject.CreateInstance<LinearTrajectoryAsset>();
            try
            {
                using KineticManager manager = NewMainManager();

                KineticObject body = manager.Register(asset).Add(new KineticInput(float3.zero, new float3(6f, 0f, 0f)));

                manager.Tick(1f);
                Assert.That(body.State.Position.x, Is.EqualTo(2f).Within(Tolerance), "1 s of the default 3 s clip.");

                UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(asset);
                serialized.FindProperty("trajectory.DurationSeconds").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(asset.Trajectory.DurationSeconds, Is.EqualTo(2f).Within(Tolerance), "The asset now authors a 2 s clip.");

                manager.Refresh(asset);
                manager.Tick(1f);

                Assert.That(body.State.Position.x, Is.EqualTo(6f).Within(Tolerance), "2 s elapsed of the refreshed 2 s clip.");
                Assert.AreEqual(KineticStatus.Completed, body.Status);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Refresh_UnregisteredAsset_Throws()
        {
            LinearTrajectoryAsset asset = ScriptableObject.CreateInstance<LinearTrajectoryAsset>();
            try
            {
                using KineticManager manager = NewMainManager();

                Assert.Throws<KeyNotFoundException>(() => manager.Refresh(asset));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        #endregion
    }
}
