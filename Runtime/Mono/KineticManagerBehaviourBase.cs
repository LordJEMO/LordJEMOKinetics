using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Scene composition root for one <see cref="KineticManager"/>: <see cref="Update"/> ticks it,
    ///     <see cref="LateUpdate"/> writes the transforms, <see cref="OnDestroy"/> disposes it. The concrete
    ///     subclass picks the backend in <see cref="CreateGroupBuilder"/>. The manager is exposed through three
    ///     segregated views — hand each component only the one it needs: <see cref="Manager"/> (frame loop),
    ///     <see cref="Registry"/> (register / look up emitters), <see cref="TrajectoryEditor"/> (controlled updates).
    ///
    ///     Also carries a small demo harness (trajectory assets + spawn params + a throwaway cube pool) that
    ///     the custom inspector drives with buttons. Regular game code ignores the harness.
    /// </summary>
    public abstract class KineticManagerBehaviourBase : MonoBehaviour
    {
        [Tooltip("Initial per-trajectory capacity for the native buffers.")]
        [SerializeField] [Min(1)] private int groupCapacity = 64;

        [Header("Demo harness (inspector buttons)")]
        [Tooltip("Linear trajectory assets to spawn into. Empty → one default LinearTrajectory.")]
        [SerializeField] private TrajectoryAssetBase[] trajectoryAssets;

        [SerializeField] [Min(0)] private int spawnCount = 100;

        [SerializeField] [Min(0f)] private float spawnScatter = 15f;

        [SerializeField] [Min(0.01f)] private float bodyScale = 0.5f;

        private KineticManager kinetic;
        private KineticTrajectoryKey<LinearTrajectory, KineticInput> defaultKey;
        private readonly List<KineticObject> demoEntries = new List<KineticObject>();
        private readonly List<GameObject> demoBodies = new List<GameObject>();

        /// <summary>Frame loop view. Created lazily so the inspector can read it before <see cref="Awake"/>.</summary>
        public IKineticManager Manager => Kinetic;

        /// <summary>Registration and emitter lookup view.</summary>
        public IKineticTrajectoryRegistry Registry => Kinetic;

        /// <summary>Controlled trajectory update view — give it to one dedicated helper only.</summary>
        public IKineticTrajectoryEditor TrajectoryEditor => Kinetic;

        private KineticManager Kinetic
        {
            get
            {
                if (kinetic == null)
                {
                    kinetic = new KineticManager(CreateGroupBuilder(groupCapacity));
                }

                return kinetic;
            }
        }

        /// <summary>Builds the backend-specific group builder.</summary>
        protected abstract IKineticTrajectoryGroupBuilder CreateGroupBuilder(int capacity);

        private void Awake()
        {
            _ = Kinetic;
        }

        private void Update()
        {
            kinetic?.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            kinetic?.ApplyTransforms();
        }

        private void OnDestroy()
        {
            ClearDemo();
            kinetic?.Dispose();
            kinetic = null;
        }

        #region demo harness

        /// <summary>
        ///     Registers every <see cref="LinearTrajectoryAsset"/> in <see cref="trajectoryAssets"/> (or one default
        ///     <see cref="LinearTrajectory"/> key) and spawns <see cref="spawnCount"/> cubes into each. Building
        ///     twice reuses the same trajectories and just adds more entries. Other assets are skipped with a
        ///     warning — the harness only knows how to author origin/target entries.
        /// </summary>
        public void BuildDemo()
        {
            bool built = false;

            if (trajectoryAssets != null)
            {
                for (int i = 0; i < trajectoryAssets.Length; i++)
                {
                    TrajectoryAssetBase asset = trajectoryAssets[i];
                    if (asset == null)
                    {
                        continue;
                    }

                    if (asset is not ITrajectoryData<LinearTrajectory, KineticInput> authored)
                    {
                        Debug.LogWarning($"[{name}] '{asset.name}' is not a Linear Trajectory asset; the demo harness only spawns origin/target entries. Skipped.", this);
                        continue;
                    }

                    SpawnInto(Registry.Register(authored), spawnCount);
                    built = true;
                }
            }

            if (!built)
            {
                if (defaultKey == null)
                {
                    defaultKey = new KineticTrajectoryKey<LinearTrajectory, KineticInput>(LinearTrajectory.OverTime(3f));
                }

                SpawnInto(Registry.Register(defaultKey), spawnCount);
            }
        }

        /// <summary>Spawns <paramref name="count"/> throwaway cubes and emits one entry per cube.</summary>
        public void SpawnInto(IKineticEmitter<KineticInput> emitter, int count)
        {
            if (emitter == null || count <= 0)
            {
                return;
            }

            float3 centre = transform.position;
            for (int i = 0; i < count; i++)
            {
                float3 origin = centre + (float3)(UnityEngine.Random.insideUnitSphere * spawnScatter);
                float3 target = centre + (float3)(UnityEngine.Random.insideUnitSphere * spawnScatter);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(transform, false);
                body.transform.localScale = new Vector3(bodyScale, bodyScale, bodyScale);
                body.transform.position = origin;
                demoBodies.Add(body);

                demoEntries.Add(emitter.Add(new KineticInput(origin, target), body.transform));
            }
        }

        /// <summary>Removes every demo entry and destroys the spawned cubes. The trajectories stay registered, empty.</summary>
        public void ClearDemo()
        {
            for (int i = 0; i < demoEntries.Count; i++)
            {
                demoEntries[i].Remove();
            }

            demoEntries.Clear();

            for (int i = 0; i < demoBodies.Count; i++)
            {
                if (demoBodies[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(demoBodies[i]);
                    }
                    else
                    {
                        DestroyImmediate(demoBodies[i]);
                    }
                }
            }

            demoBodies.Clear();
        }

        #endregion
    }
}
