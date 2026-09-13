using Unity.Mathematics;
using UnityEngine;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The per-entry surface — the "kinetic object" (a bullet, a unit). Returned by
    ///     <see cref="IKineticEmitter{TInput}.Add"/>; the data itself lives in the manager's native storage,
    ///     not here.
    /// </summary>
    public interface IKineticObject
    {
        /// <summary>This entry's handle inside its trajectory.</summary>
        KineticHandle Handle { get; }

        /// <summary>False once the entry has been removed (or the handle never referred to a live entry).</summary>
        bool IsValid { get; }

        /// <summary>The entry's current pose, clock and status.</summary>
        KineticPlayback State { get; }

        /// <summary>Shorthand for <see cref="State"/>.<see cref="KineticPlayback.Status"/>.</summary>
        KineticStatus Status { get; }

        /// <summary>The associated transform, or <c>null</c> if the entry is headless (data only).</summary>
        Transform Transform { get; }

        /// <summary>The entry's whole input. Must be the input type its trajectory was added with.</summary>
        TInput GetInput<TInput>()
            where TInput : unmanaged, IKineticInput;

        /// <summary>Replaces the entry's whole input. Must be the input type its trajectory was added with.</summary>
        void SetInput<TInput>(in TInput input)
            where TInput : unmanaged, IKineticInput;

        /// <summary>Sets <see cref="IKineticInput.Origin"/> (takes effect on the next <see cref="Replay"/>).</summary>
        void SetOrigin(float3 origin);

        /// <summary>Retargets the entry — sets <see cref="IKineticInput.Target"/>.</summary>
        void SetTarget(float3 target);

        /// <summary>Turns this entry's transform write on / off without removing the transform.</summary>
        void SetWriteTransform(bool writeTransform);

        /// <summary>Freezes this entry (the batch advance skips it).</summary>
        void Pause();

        /// <summary>Continues this entry after a <see cref="Pause"/>.</summary>
        void Resume();

        /// <summary>Rewinds the clock and re-enters <see cref="KineticStatus.Playing"/>.</summary>
        void Replay();

        /// <summary>Removes this entry from its trajectory. The trajectory itself stays.</summary>
        void Remove();
    }
}
