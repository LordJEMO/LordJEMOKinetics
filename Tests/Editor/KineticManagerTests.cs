using System.Collections.Generic;
using LordJEMO.Kinematics;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LordJEMO.Tests.EditMode.Kinematics
{
    [TestFixture, Category("Kinematics")]
    public class KineticManagerTests
    {
        private const float Tolerance = 1e-4f;

        /// <summary>A second trajectory type — parks the body at its origin forever.</summary>
        private struct HoldTrajectory : IKineticTrajectory<KineticInput>
        {
            public KineticOutput Evaluate(in KineticInput context, float time)
            {
                return new KineticOutput
                {
                    Position = context.Origin,
                    Direction = float3.zero,
                    Rotation = quaternion.identity,
                    NormalizedTime = 0f,
                    Status = KineticStatus.Playing,
                };
            }
        }

        private static KineticInput Ctx(float3 origin, float3 target)
        {
            return new KineticInput(origin, target);
        }

        private static KineticManager NewMainManager(int capacity = 16)
        {
            return new KineticManager(new KineticTrajectoryGroupMainBuilder(capacity));
        }

        private static KineticManager NewJobsManager(int capacity = 64)
        {
            return new KineticManager(new KineticTrajectoryGroupJobsBuilder(capacity));
        }

        private static KineticTrajectoryKey<LinearTrajectory, KineticInput> Key(LinearTrajectory trajectory)
        {
            return new KineticTrajectoryKey<LinearTrajectory, KineticInput>(trajectory);
        }

        [Test]
        public void CreateGroup_ThenAdd_ReturnsValidObject()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            KineticObject a = linear.Add(Ctx(float3.zero, new float3(10f, 0f, 0f)));

            Assert.IsTrue(a.IsValid);
            Assert.AreEqual(1, manager.TrajectoryCount);
            Assert.AreEqual(1, manager.Count);
            Assert.AreEqual(KineticStatus.Playing, a.Status);
        }

        [Test]
        public void OneGroup_ManyEntries_ShareItsTrajectory()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            KineticObject a = linear.Add(Ctx(float3.zero, new float3(2f, 0f, 0f)));
            KineticObject b = linear.Add(Ctx(new float3(0f, 3f, 0f), new float3(0f, 3f, 4f)));
            KineticObject c = linear.Add(Ctx(float3.zero, new float3(0f, 0f, 8f)));

            Assert.AreEqual(3, manager.Count);

            manager.Tick(1f);

            Assert.That(a.State.Position.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(b.State.Position.z, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(c.State.Position.z, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Remove_MidGroup_KeepsOtherHandlesValid()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            KineticObject a = linear.Add(Ctx(float3.zero, new float3(1f, 0f, 0f)));
            KineticObject b = linear.Add(Ctx(float3.zero, new float3(2f, 0f, 0f)));
            KineticObject c = linear.Add(Ctx(float3.zero, new float3(3f, 0f, 0f)));

            b.Remove();

            Assert.IsFalse(b.IsValid, "Removed handle is stale.");
            Assert.IsTrue(a.IsValid);
            Assert.IsTrue(c.IsValid);
            Assert.AreEqual(2, manager.Count);

            manager.Tick(1f);
            Assert.That(c.State.Position.x, Is.EqualTo(1.5f).Within(Tolerance), "c advanced along its own input.");
        }

        [Test]
        public void StaleHandle_AfterRemove_IsIgnored()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            KineticObject a = linear.Add(Ctx(float3.zero, new float3(1f, 0f, 0f)));
            a.Remove();

            Assert.IsFalse(a.IsValid);
            Assert.AreEqual(default(KineticPlayback), a.State);
            Assert.DoesNotThrow(() => a.Remove());
            Assert.DoesNotThrow(() => a.SetTarget(float3.zero));
        }

        [Test]
        public void Pause_FreezesOnlyThatEntry()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(4f)));
            KineticObject held = linear.Add(Ctx(float3.zero, new float3(4f, 0f, 0f)));
            KineticObject moving = linear.Add(Ctx(float3.zero, new float3(4f, 0f, 0f)));

            manager.Tick(1f);
            held.Pause();
            float3 frozen = held.State.Position;

            manager.Tick(1f);

            Assert.That(math.distance(held.State.Position, frozen), Is.LessThan(Tolerance), "Paused entry did not move.");
            Assert.That(moving.State.Position.x, Is.EqualTo(2f).Within(Tolerance), "Other entry kept advancing.");
        }

        [Test]
        public void TwoTrajectoryTypes_Coexist()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> linear = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            IKineticEmitter<KineticInput> hold = manager.Register(new KineticTrajectoryKey<HoldTrajectory, KineticInput>(new HoldTrajectory()));

            KineticObject moving = linear.Add(Ctx(float3.zero, new float3(10f, 0f, 0f)));
            KineticObject parked = hold.Add(Ctx(new float3(0f, 7f, 0f), float3.zero));

            Assert.AreEqual(2, manager.TrajectoryCount);
            Assert.AreEqual(2, manager.Count);

            manager.Tick(1f);

            Assert.That(moving.State.Position.x, Is.EqualTo(5f).Within(Tolerance), "Linear entry at 50 percent.");
            Assert.That(math.distance(parked.State.Position, new float3(0f, 7f, 0f)), Is.LessThan(Tolerance), "Hold entry stayed at its origin.");
        }

        [Test]
        public void EmptyTrajectory_PersistsAfterLastEntryRemoved()
        {
            using KineticManager manager = NewMainManager();

            KineticTrajectoryKey<LinearTrajectory, KineticInput> key = Key(LinearTrajectory.OverTime(2f));
            IKineticEmitter<KineticInput> linear = manager.Register(key);
            linear.Add(Ctx(float3.zero, new float3(2f, 0f, 0f))).Remove();

            Assert.AreEqual(0, manager.Count);
            Assert.AreEqual(1, manager.TrajectoryCount, "The trajectory stays, empty.");
            Assert.IsTrue(manager.IsRegistered(key));

            KineticObject again = linear.Add(Ctx(float3.zero, new float3(2f, 0f, 0f)));
            Assert.IsTrue(again.IsValid, "An empty trajectory still accepts entries.");
        }

        [Test]
        public void Register_SameKeyTwice_ReturnsSameEmitter()
        {
            using KineticManager manager = NewMainManager();

            KineticTrajectoryKey<LinearTrajectory, KineticInput> key = Key(LinearTrajectory.OverTime(2f));

            Assert.AreSame(manager.Register(key), manager.Register(key));
            Assert.AreEqual(1, manager.TrajectoryCount);
        }

        [Test]
        public void Register_EqualValuesDifferentKeys_CreatesSeparateTrajectories()
        {
            using KineticManager manager = NewMainManager();

            IKineticEmitter<KineticInput> first = manager.Register(Key(LinearTrajectory.OverTime(2f)));
            IKineticEmitter<KineticInput> second = manager.Register(Key(LinearTrajectory.OverTime(2f)));

            Assert.AreNotSame(first, second);
            Assert.AreEqual(2, manager.TrajectoryCount);
        }

        [Test]
        public void GetEmitter_FromSeparateComponent_ReturnsRegisteredEmitter()
        {
            using KineticManager manager = NewMainManager();
            IKineticTrajectoryRegistry compositionRoot = manager;
            IKineticTrajectoryRegistry gun = manager;

            KineticTrajectoryKey<LinearTrajectory, KineticInput> sharedKey = Key(LinearTrajectory.OverTime(2f));
            IKineticEmitter<KineticInput> registered = compositionRoot.Register(sharedKey);

            Assert.AreSame(registered, gun.GetEmitter(sharedKey));
        }

        [Test]
        public void GetEmitter_UnregisteredKey_Throws()
        {
            using KineticManager manager = NewMainManager();

            Assert.Throws<KeyNotFoundException>(() => manager.GetEmitter(Key(LinearTrajectory.OverTime(2f))));
        }

        [Test]
        public void TryGetEmitter_UnregisteredKey_ReturnsFalse()
        {
            using KineticManager manager = NewMainManager();

            Assert.IsFalse(manager.TryGetEmitter(Key(LinearTrajectory.OverTime(2f)), out IKineticEmitter<KineticInput> emitter));
            Assert.IsNull(emitter);
        }

        [Test]
        public void Update_CodeKey_RetunesLiveEntries()
        {
            using KineticManager manager = NewMainManager();

            KineticTrajectoryKey<LinearTrajectory, KineticInput> key = Key(LinearTrajectory.OverTime(4f));
            KineticObject body = manager.Register(key).Add(Ctx(float3.zero, new float3(4f, 0f, 0f)));

            manager.Tick(1f);
            Assert.That(body.State.Position.x, Is.EqualTo(1f).Within(Tolerance), "25 percent of a 4 s clip.");

            manager.Update(key, LinearTrajectory.OverTime(2f));
            manager.Tick(1f);

            Assert.That(body.State.Position.x, Is.EqualTo(4f).Within(Tolerance), "2 s elapsed of the new 2 s clip.");
            Assert.AreEqual(KineticStatus.Completed, body.Status);
            Assert.That(key.Trajectory.DurationSeconds, Is.EqualTo(2f).Within(Tolerance), "The key holds the new value.");
        }

        [Test]
        public void Update_OnlyAffectsItsOwnKey()
        {
            using KineticManager manager = NewMainManager();

            KineticTrajectoryKey<LinearTrajectory, KineticInput> updated = Key(LinearTrajectory.OverTime(4f));
            KineticTrajectoryKey<LinearTrajectory, KineticInput> untouched = Key(LinearTrajectory.OverTime(4f));
            KineticObject fast = manager.Register(updated).Add(Ctx(float3.zero, new float3(4f, 0f, 0f)));
            KineticObject slow = manager.Register(untouched).Add(Ctx(float3.zero, new float3(4f, 0f, 0f)));

            manager.Update(updated, LinearTrajectory.OverTime(2f));
            manager.Tick(1f);

            Assert.That(fast.State.Position.x, Is.EqualTo(2f).Within(Tolerance), "Updated key flies the 2 s clip.");
            Assert.That(slow.State.Position.x, Is.EqualTo(1f).Within(Tolerance), "Equal-valued key keeps the 4 s clip.");
        }

        [Test]
        public void Update_UnregisteredKey_Throws_AndLeavesKeyUnchanged()
        {
            using KineticManager manager = NewMainManager();

            KineticTrajectoryKey<LinearTrajectory, KineticInput> key = Key(LinearTrajectory.OverTime(4f));

            Assert.Throws<KeyNotFoundException>(() => manager.Update(key, LinearTrajectory.OverTime(2f)));
            Assert.That(key.Trajectory.DurationSeconds, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void MainAndJobs_Groups_AgreeWithEachOther_AndWithDirectEvaluate()
        {
            const int count = 64;
            const float dt = 0.05f;
            const int steps = 20;

            using KineticManager main = NewMainManager(count);
            using KineticManager jobs = NewJobsManager(count);

            IKineticEmitter<KineticInput> mainEmitter = main.Register(Key(LinearTrajectory.OverTime(3f)));
            IKineticEmitter<KineticInput> jobEmitter = jobs.Register(Key(LinearTrajectory.OverTime(3f)));

            KineticObject[] mainObjects = new KineticObject[count];
            KineticObject[] jobObjects = new KineticObject[count];
            KineticInput[] ctxs = new KineticInput[count];

            for (int i = 0; i < count; i++)
            {
                float3 origin = new float3(i, 0f, 0f);
                float3 target = new float3(i, 5f + i, 0f);
                ctxs[i] = Ctx(origin, target);

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
                float3 expected = LinearTrajectory.OverTime(3f).Evaluate(ctxs[i], total).Position;

                Assert.That(math.distance(mainObjects[i].State.Position, expected), Is.LessThan(Tolerance), $"Main vs direct at {i}.");
                Assert.That(math.distance(jobObjects[i].State.Position, expected), Is.LessThan(Tolerance), $"Jobs vs direct at {i}.");
            }
        }

        [Test]
        public void ApplyTransforms_HonoursWriteFlag_JobsBackend()
        {
            GameObject writeGo = new GameObject("kin_write");
            GameObject skipGo = new GameObject("kin_skip");
            try
            {
                using KineticManager jobs = NewJobsManager();

                IKineticEmitter<KineticInput> linear = jobs.Register(Key(LinearTrajectory.OverTime(2f)));
                float3 target = new float3(6f, 0f, 0f);

                linear.Add(Ctx(float3.zero, target), writeGo.transform, writeTransform: true);
                KineticObject skipped = linear.Add(Ctx(float3.zero, target), skipGo.transform, writeTransform: false);

                jobs.Tick(1f);
                jobs.ApplyTransforms();

                Assert.That(math.distance((float3)writeGo.transform.position, new float3(3f, 0f, 0f)), Is.LessThan(Tolerance), "Write entry moved its transform.");
                Assert.That(math.distance((float3)skipGo.transform.position, float3.zero), Is.LessThan(Tolerance), "Skip entry left its transform alone.");
                Assert.That(skipped.State.Position.x, Is.EqualTo(3f).Within(Tolerance), "Skip entry's State still advanced.");

                skipped.SetWriteTransform(true);
                jobs.ApplyTransforms();
                Assert.That(math.distance((float3)skipGo.transform.position, new float3(3f, 0f, 0f)), Is.LessThan(Tolerance), "Skip entry now synced.");
            }
            finally
            {
                Object.DestroyImmediate(writeGo);
                Object.DestroyImmediate(skipGo);
            }
        }
    }
}
