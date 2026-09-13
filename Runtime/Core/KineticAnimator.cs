using System;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Drives one <see cref="KineticPlayback"/> along an injected motion law. The law is the type
    ///     parameter <typeparamref name="TTrajectory"/> and the data it reads is
    ///     <typeparamref name="TContext"/> — "injecting a different motion model" means choosing
    ///     different type arguments and assigning the <see cref="Trajectory"/> / <see cref="Context"/>
    ///     fields.
    ///
    ///     Constrained to the <em>base</em> <see cref="IKineticTrajectory{TContext}"/> so unbounded
    ///     laws (pursuit, orbit) work here too. Clip-only affordances (jump to end, scrub to a
    ///     fraction) are extension methods in <see cref="KineticClipAnimator"/>.
    ///
    ///     A <c>struct</c>, not a class, and constrained to <c>unmanaged</c>, so the whole animator is
    ///     blittable: it can be a <see cref="UnityEngine.MonoBehaviour"/> field, an element of a
    ///     <see cref="Unity.Collections.NativeArray{T}"/>, or live inside a job. Because the type
    ///     arguments are concrete at every use site, Burst monomorphises
    ///     <see cref="IKineticTrajectory{TContext}.Evaluate"/> to a direct, inlinable call — no
    ///     boxing, no virtual dispatch.
    /// </summary>
    [Serializable]
    public struct KineticAnimator<TTrajectory, TContext> : IKineticAnimator
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        /// <summary>The injected motion law. Assign before the first <see cref="Reset"/> / <see cref="Tick"/>.</summary>
        public TTrajectory Trajectory;

        /// <summary>The data the law is evaluated against. Mutate to retarget mid-flight.</summary>
        public TContext Context;

        /// <summary>The pose, clock and status this animator maintains. Read after <see cref="Tick"/>.</summary>
        public KineticPlayback State;

        readonly KineticPlayback IKineticAnimator.State => State;

        /// <summary>Snaps back to the trajectory start, clears the clock, and enters <see cref="KineticStatus.Playing"/>.</summary>
        public void Reset()
        {
            State.Time = 0f;

            KineticOutput sample = Trajectory.Evaluate(in Context, 0f);

            State.Position = sample.Position;
            State.Direction = sample.Direction;
            State.Rotation = sample.Rotation;
            State.Status = KineticStatus.Playing;
        }

        /// <summary>Freezes the clock. No effect unless currently <see cref="KineticStatus.Playing"/>.</summary>
        public void Pause()
        {
            if (State.Status == KineticStatus.Playing)
            {
                State.Status = KineticStatus.Paused;
            }
        }

        /// <summary>Un-freezes the clock. No effect unless currently <see cref="KineticStatus.Paused"/>.</summary>
        public void Resume()
        {
            if (State.Status == KineticStatus.Paused)
            {
                State.Status = KineticStatus.Playing;
            }
        }

        /// <summary>
        ///     Advances the clock by <paramref name="deltaTime"/> and re-samples the pose. A no-op
        ///     while <see cref="KineticStatus.Paused"/> or <see cref="KineticStatus.Completed"/>.
        ///     Whether one big step and several small steps agree is a property of
        ///     <typeparamref name="TTrajectory"/> — <see cref="LinearTrajectory"/> is exact; a future
        ///     integrating law should document its own tolerance.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (State.Status == KineticStatus.Paused || State.Status == KineticStatus.Completed)
            {
                return;
            }

            State.Time += deltaTime;

            KineticOutput sample = Trajectory.Evaluate(in Context, State.Time);

            State.Position = sample.Position;
            State.Direction = sample.Direction;
            State.Rotation = sample.Rotation;
            State.Status = sample.Status;
        }
    }
}
