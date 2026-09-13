using System;
using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     One <em>rule of movement</em> plus the bodies flying it — the inner layer behind
    ///     <see cref="IKineticManager"/>. Every entry carries its own input + <see cref="KineticPlayback"/>;
    ///     they all share the group's single <c>Trajectory</c>. Game code never holds a group: the manager
    ///     creates one per registered key (via an <see cref="IKineticTrajectoryGroupBuilder"/>) and entries
    ///     arrive through that key's <see cref="IKineticEmitter{TInput}"/>.
    ///
    ///     This tier is <em>input-agnostic</em> — everything that can be done without naming the input
    ///     type, so the manager can keep a heterogeneous list. Entries are addressed by
    ///     <see cref="KineticHandle"/>, stable across removals of other entries (the group
    ///     swap-removes internally); a handle is only valid for the group that issued it. Transforms
    ///     are borrowed: the group never instantiates or destroys one.
    /// </summary>
    public interface IKineticTrajectoryGroup : IDisposable
    {
        /// <summary>The <see cref="IKineticInput"/> type entries of this group are authored with.</summary>
        Type InputType { get; }

        /// <summary>Live entries.</summary>
        int Count { get; }

        /// <summary>Advance every entry's data by <paramref name="deltaTime"/>.</summary>
        void Tick(float deltaTime);

        /// <summary>Write the resolved pose onto every entry whose transform-sync is on.</summary>
        void ApplyTransforms();

        /// <summary>Removes the entry (detaches, does not destroy, its transform). No-op if the handle is stale.</summary>
        void Remove(KineticHandle handle);

        bool IsValid(KineticHandle handle);

        KineticPlayback GetState(KineticHandle handle);

        Transform GetTransform(KineticHandle handle);

        /// <summary>Sets the entry input's <see cref="IKineticInput.Origin"/>.</summary>
        void SetOrigin(KineticHandle handle, float3 origin);

        /// <summary>Sets the entry input's <see cref="IKineticInput.Target"/> (derived, for inputs that have no literal destination).</summary>
        void SetTarget(KineticHandle handle, float3 target);

        void SetWriteTransform(KineticHandle handle, bool writeTransform);

        void Pause(KineticHandle handle);

        void Resume(KineticHandle handle);

        void Replay(KineticHandle handle);
    }

    /// <summary>Input-typed tier: adding entries and replacing a whole input.</summary>
    public interface IKineticTrajectoryGroup<TContext> : IKineticTrajectoryGroup
        where TContext : unmanaged, IKineticInput
    {
        /// <summary>
        ///     Adds an entry. <paramref name="transform"/> may be <c>null</c> (headless).
        ///     <paramref name="writeTransform"/> <c>false</c> keeps the transform but leaves it
        ///     untouched by <see cref="IKineticTrajectoryGroup.ApplyTransforms"/> (the
        ///     <see cref="KineticPlayback"/> still advances).
        /// </summary>
        KineticObject Add(in TContext context, Transform transform = null, bool writeTransform = true);

        /// <summary>The entry's input; <c>default</c> if the handle is stale.</summary>
        TContext GetContext(KineticHandle handle);

        /// <summary>Replaces the entry's input wholesale.</summary>
        void SetContext(KineticHandle handle, in TContext context);
    }

    /// <summary>Fully typed tier: reads the group's single rule.</summary>
    public interface IKineticTrajectoryGroup<TTrajectory, TContext> : IKineticTrajectoryGroup<TContext>
        where TContext : unmanaged, IKineticInput
        where TTrajectory : unmanaged, IKineticTrajectory<TContext>
    {
        /// <summary>The one rule every entry in this group flies.</summary>
        TTrajectory Trajectory { get; }

        /// <summary>Replaces the rule in place; live entries fly it from the next tick. Called only by the manager's <see cref="IKineticTrajectoryEditor"/> path.</summary>
        void SetTrajectory(in TTrajectory trajectory);
    }
}
