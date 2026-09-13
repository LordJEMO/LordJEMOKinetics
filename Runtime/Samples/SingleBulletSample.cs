using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The smallest end-to-end use of the kinematic system: press Space, one bullet flies.
    ///
    ///     Setup: put this on an empty GameObject, assign <see cref="bulletTemplate"/> (any Transform
    ///     in the scene, e.g. a sphere) and <see cref="trajectoryAsset"/> (create one from
    ///     Assets > Create > LordJEMO > Kinematics). Press Play, then Space: a clone of the template
    ///     leaves <see cref="startPosition"/> travelling along <see cref="direction"/>.
    ///
    ///     The four steps every kinematic scene follows:
    ///     <list type="number">
    ///       <item>create a manager and register the asset as a trajectory, keeping its emitter  (Start, once)</item>
    ///       <item>emit one entry: an input + the transform to drive  (Fire, per shot)</item>
    ///       <item>advance the data every frame, then read status  (Update -> Tick)</item>
    ///       <item>write the resolved pose onto the transforms  (LateUpdate -> ApplyTransforms)</item>
    ///     </list>
    ///
    ///     This script is its own composition root. In a larger scene the registration would live where the
    ///     manager is created, and a gun component would only receive (or look up) the emitter.
    ///     The manager never creates or destroys a Transform - this script owns the clones, which is what lets
    ///     a real game feed it a pooled bullet instead.
    /// </summary>
    public class SingleBulletSample : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Cloned once per shot. Any Transform in the scene.")]
        [SerializeField] private Transform bulletTemplate;

        [Tooltip("A Constant Velocity Trajectory or Linear Trajectory asset.")]
        [SerializeField] private TrajectoryAssetBase trajectoryAsset;

        [Header("Shot")]
        [SerializeField] private KeyCode fireKey = KeyCode.Space;

        [SerializeField] private Vector3 startPosition = Vector3.zero;

        [SerializeField] private Vector3 direction = new Vector3(0f, 0f, 1f);

        [Tooltip("Units per second. Used by a Constant Velocity asset.")]
        [SerializeField] [Min(0f)] private float speed = 10f;

        [Tooltip("Used only by a Linear asset: how far along the direction its target sits.")]
        [SerializeField] [Min(0f)] private float linearRange = 50f;

        [Tooltip("Destroy a bullet once it reports Completed. A Constant Velocity rule with no Lifetime never does.")]
        [SerializeField] private bool despawnOnArrival = true;

        private IKineticManager manager;

        // Exactly one of these is set - whichever input the assigned asset's trajectory takes.
        private IKineticEmitter<KineticVelocityInput> velocityEmitter;
        private IKineticEmitter<KineticInput> pointEmitter;

        private readonly List<KineticObject> shots = new List<KineticObject>();
        private readonly List<GameObject> bodies = new List<GameObject>();

        private void Start()
        {
            if (bulletTemplate == null || trajectoryAsset == null)
            {
                Debug.LogWarning($"[{name}] Assign both Bullet Template and Trajectory Asset.", this);
                enabled = false;
                return;
            }

            // Step 1 - one manager (the simple, single-threaded backend) and the asset registered as a trajectory.
            KineticManager kinetic = new KineticManager(new KineticTrajectoryGroupMainBuilder());
            manager = kinetic;

            if (trajectoryAsset is ITrajectoryData<ConstantVelocityTrajectory, KineticVelocityInput> velocityData)
            {
                velocityEmitter = kinetic.Register(velocityData);
            }
            else if (trajectoryAsset is ITrajectoryData<LinearTrajectory, KineticInput> pointData)
            {
                pointEmitter = kinetic.Register(pointData);
            }
            else
            {
                Debug.LogWarning($"[{name}] {trajectoryAsset.name} is not a trajectory this sample handles.", this);
                enabled = false;
            }
        }

        /// <summary>Clones the template and emits the clone as one new entry.</summary>
        public void Fire()
        {
            float3 origin = startPosition;
            float3 heading = math.normalizesafe(direction, new float3(0f, 0f, 1f));

            GameObject body = Instantiate(bulletTemplate.gameObject, origin, Quaternion.identity, transform);
            body.name = $"Bullet_{bodies.Count}";
            body.SetActive(true);
            bodies.Add(body);

            // Step 2 - one entry. The input the trajectory takes decides how a direction is expressed.
            if (velocityEmitter != null)
            {
                // A velocity law takes a launch point and a velocity vector.
                shots.Add(velocityEmitter.Add(new KineticVelocityInput(origin, heading * speed), body.transform));
            }
            else
            {
                // A point-to-point law needs a destination, so project one along the heading.
                shots.Add(pointEmitter.Add(new KineticInput(origin, origin + (heading * linearRange)), body.transform));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(fireKey))
            {
                Fire();
            }

            // Step 3 - advance the data, then read the status.
            manager?.Tick(Time.deltaTime);

            if (despawnOnArrival)
            {
                DespawnArrived();
            }
        }

        // Step 4 - write the resolved pose onto every bullet transform.
        private void LateUpdate()
        {
            manager?.ApplyTransforms();
        }

        private void DespawnArrived()
        {
            // Backwards: removing an entry swap-removes it inside its trajectory, and we mirror that here.
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (shots[i].Status != KineticStatus.Completed)
                {
                    continue;
                }

                shots[i].Remove();          // detaches the entry - never touches the Transform
                Destroy(bodies[i]);         // this script owns the clone, so this script destroys it

                shots.RemoveAt(i);
                bodies.RemoveAt(i);
            }
        }

        private void OnDestroy()
        {
            manager?.Dispose();
            manager = null;
        }
    }
}
