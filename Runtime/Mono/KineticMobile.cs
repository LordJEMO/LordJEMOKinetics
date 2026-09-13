using System;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     MonoBehaviour adapter. Holds an injected <see cref="IKineticTrajectory{TContext}"/> (over
    ///     the plain <see cref="KineticInput"/>) plus a context, advances a clock every frame, and
    ///     writes the sampled pose onto this object's <see cref="Transform"/>. The motion math lives
    ///     entirely in the trajectory implementation — this class only does Unity lifecycle,
    ///     serialization, applying the pose, and the play / pause / resume state.
    ///
    ///     The trajectory is a <c>[SerializeReference]</c> field, so a designer picks the concrete
    ///     law (<see cref="LinearTrajectory"/>, or any future implementation) from the inspector's
    ///     type dropdown. That means one managed interface call per frame per mobile — fine for the
    ///     Mono path. A pool of thousands of bodies belongs on
    ///     <see cref="KineticUpdateJob{TTrajectory, TContext}"/> with the same structs (see
    ///     <see cref="KineticBatchSample"/>); for a MonoBehaviour with zero per-frame dispatch,
    ///     subclass with a concrete <see cref="KineticAnimator{TTrajectory, TContext}"/> instead.
    /// </summary>
    public class KineticMobile : MonoBehaviour
    {
        [SerializeReference]
        [Tooltip("The motion law. Pick a concrete implementation from the dropdown.")]
        private IKineticTrajectory<KineticInput> trajectory = LinearTrajectory.AtSpeed(10f);

        [SerializeField] private KineticInput context;

        [Tooltip("On Play, overwrite context.Origin with this transform's current world position.")]
        [SerializeField] private bool useTransformAsOrigin = true;

        [Tooltip("Apply the sampled rotation to the transform.")]
        [SerializeField] private bool faceDirection = true;

        [Tooltip("Begin moving as soon as the component is enabled.")]
        [SerializeField] private bool playOnEnable = true;

        private KineticPlayback state;
        private bool completedRaised;

        /// <summary>Raised once, on the frame the motion reaches its end.</summary>
        public event Action<KineticMobile> Completed;

        /// <summary>The injected motion law. Assign before <see cref="Play"/> to swap behaviour at runtime.</summary>
        public IKineticTrajectory<KineticInput> Trajectory
        {
            get { return trajectory; }
            set { trajectory = value; }
        }

        /// <summary>The endpoints the law is evaluated against. Mutate <see cref="KineticInput.Target"/> to retarget.</summary>
        public KineticInput Context
        {
            get { return context; }
            set { context = value; }
        }

        /// <summary>Live pose, clock and status. Read-only view for callers.</summary>
        public KineticPlayback State
        {
            get { return state; }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        /// <summary>Anchors the origin (optionally to the current transform), resets the clock, and starts moving.</summary>
        public void Play()
        {
            if (trajectory == null)
            {
                return;
            }

            if (useTransformAsOrigin)
            {
                context.Origin = transform.position;
            }

            state = default;
            Sample(0f);
            ApplyPose();

            completedRaised = false;
        }

        /// <summary>Resets to the start pose and parks it (<see cref="KineticStatus.Idle"/>) for pooled reuse before a later <see cref="Play"/>.</summary>
        public void ResetMotion()
        {
            Play();
            state.Status = KineticStatus.Idle;
        }

        /// <summary>Freezes motion; <see cref="Update"/> stops advancing until <see cref="Resume"/>.</summary>
        public void Pause()
        {
            if (state.Status == KineticStatus.Playing)
            {
                state.Status = KineticStatus.Paused;
            }
        }

        /// <summary>Continues after a <see cref="Pause"/>.</summary>
        public void Resume()
        {
            if (state.Status == KineticStatus.Paused)
            {
                state.Status = KineticStatus.Playing;
            }
        }

        /// <summary>
        ///     Jumps to the end pose and marks the motion complete. Only meaningful for a bounded
        ///     law — a no-op when the injected trajectory is not an <see cref="IKineticClip{TContext}"/>.
        /// </summary>
        public void SnapToEnd()
        {
            if (trajectory is not IKineticClip<KineticInput> clip)
            {
                return;
            }

            Sample(clip.Duration(in context));
            state.Status = KineticStatus.Completed;
            ApplyPose();

            RaiseCompletedOnce();
        }

        private void Update()
        {
            if (trajectory == null || state.Status != KineticStatus.Playing)
            {
                return;
            }

            Sample(state.Time + Time.deltaTime);
            ApplyPose();

            if (state.Status == KineticStatus.Completed)
            {
                RaiseCompletedOnce();
            }
        }

        private void Sample(float time)
        {
            state.Time = time;

            KineticOutput sample = trajectory.Evaluate(in context, time);
            state.Position = sample.Position;
            state.Direction = sample.Direction;
            state.Rotation = sample.Rotation;
            state.Status = sample.Status;
        }

        private void ApplyPose()
        {
            if (faceDirection)
            {
                transform.SetPositionAndRotation(state.Position, state.Rotation);
            }
            else
            {
                transform.position = state.Position;
            }
        }

        private void RaiseCompletedOnce()
        {
            if (completedRaised)
            {
                return;
            }

            completedRaised = true;
            Completed?.Invoke(this);
        }
    }
}
