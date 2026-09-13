using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Code-only sample for the manager layer. Registers <b>two</b> <see cref="LinearTrajectory"/> rules
    ///     under two <see cref="KineticTrajectoryKey{TTrajectory, TInput}"/>s (two trajectories even when the
    ///     values are equal), splits a self-owned cube pool between their emitters, and every frame does
    ///     <see cref="IKineticManager.Tick"/> (step 2) → reads status (step 3) →
    ///     <see cref="IKineticManager.ApplyTransforms"/> (step 4). The manager never touches the cubes'
    ///     lifetime.
    ///
    ///     For an inspector-driven version use <c>KineticManagerMainBehaviour</c> /
    ///     <c>KineticManagerJobsBehaviour</c> instead.
    /// </summary>
    public class KineticBatchSample : MonoBehaviour
    {
        [SerializeField] private KineticExecutionMode mode = KineticExecutionMode.Jobs;

        [SerializeField] [Min(0)] private int count = 200;

        [Tooltip("Optional authored rules. Empty → two inline LinearTrajectory values are used.")]
        [SerializeField] private LinearTrajectoryAsset[] trajectoryAssets;

        [SerializeField] private LinearTrajectory ruleA = LinearTrajectory.OverTime(3f);
        [SerializeField] private LinearTrajectory ruleB = LinearTrajectory.OverTime(6f);

        [SerializeField] [Min(0f)] private float scatterRadius = 20f;

        [SerializeField] private uint seed = 1234u;

        [SerializeField] private bool pingPong = true;

        [SerializeField] [Min(0.01f)] private float bodyScale = 0.5f;

        private IKineticManager manager;
        private readonly List<KineticObject> objects = new List<KineticObject>();
        private readonly List<float3> endA = new List<float3>();
        private readonly List<float3> endB = new List<float3>();
        private readonly List<bool> atB = new List<bool>();

        private void Start()
        {
            if (count <= 0)
            {
                return;
            }

            IKineticTrajectoryGroupBuilder builder = mode == KineticExecutionMode.Jobs
                ? new KineticTrajectoryGroupJobsBuilder(count)
                : new KineticTrajectoryGroupMainBuilder(count);
            KineticManager kinetic = new KineticManager(builder);
            manager = kinetic;

            IKineticEmitter<KineticInput> emitterA = kinetic.Register(new KineticTrajectoryKey<LinearTrajectory, KineticInput>(ResolveRule(0, ruleA)));
            IKineticEmitter<KineticInput> emitterB = kinetic.Register(new KineticTrajectoryKey<LinearTrajectory, KineticInput>(ResolveRule(1, ruleB)));

            Random random = new Random(seed == 0u ? 1u : seed);

            for (int i = 0; i < count; i++)
            {
                float3 origin = RandomPointInSphere(ref random);
                float3 target = RandomPointInSphere(ref random);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = $"KineticBody_{i}";
                body.transform.SetParent(transform, false);
                body.transform.localScale = new Vector3(bodyScale, bodyScale, bodyScale);
                body.transform.position = origin;

                IKineticEmitter<KineticInput> emitter = (i % 2 == 0) ? emitterA : emitterB;
                objects.Add(emitter.Add(new KineticInput(origin, target), body.transform));
                endA.Add(origin);
                endB.Add(target);
                atB.Add(false);
            }
        }

        private LinearTrajectory ResolveRule(int index, LinearTrajectory fallback)
        {
            if (trajectoryAssets != null && index < trajectoryAssets.Length && trajectoryAssets[index] != null)
            {
                return trajectoryAssets[index].Trajectory;
            }

            return fallback;
        }

        private void Update()
        {
            manager?.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (manager == null)
            {
                return;
            }

            manager.ApplyTransforms();

            if (!pingPong)
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Status != KineticStatus.Completed)
                {
                    return;
                }
            }

            for (int i = 0; i < objects.Count; i++)
            {
                bool goingToA = atB[i];
                atB[i] = !goingToA;

                float3 from = goingToA ? endB[i] : endA[i];
                float3 to = goingToA ? endA[i] : endB[i];

                objects[i].SetOrigin(from);
                objects[i].SetTarget(to);
                objects[i].Replay();
            }
        }

        private float3 RandomPointInSphere(ref Random random)
        {
            float3 direction = random.NextFloat3Direction();
            float radius = random.NextFloat(0f, scatterRadius);
            return (float3)transform.position + (direction * radius);
        }

        private void OnDestroy()
        {
            manager?.Dispose();
            manager = null;
        }
    }
}
