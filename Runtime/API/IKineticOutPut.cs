using Unity.Mathematics;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The read view shared by <see cref="KineticOutput"/> (a trajectory's pure output at one
    ///     time) and <see cref="KineticPlayback"/> (a body's running record): where it is, which way it
    ///     is heading, how it is oriented, and its lifecycle status. Managed consumers — the sample
    ///     MonoBehaviour, gizmo drawers, tests — take an <see cref="IKineticOutPut"/> and do not care
    ///     which they were handed.
    ///
    ///     The parts that are NOT shared stay on the concrete types: <see cref="KineticOutput.NormalizedTime"/>
    ///     (only a clip has one) and <see cref="KineticPlayback.Time"/> (the running clock).
    /// </summary>
    public interface IKineticOutPut
    {
        /// <summary>World-space position.</summary>
        float3 Position { get; }

        /// <summary>Unit travel direction; <c>float3.zero</c> when undefined.</summary>
        float3 Direction { get; }

        /// <summary>Orientation, decided by the trajectory.</summary>
        quaternion Rotation { get; }

        /// <summary>Lifecycle status. A <see cref="KineticOutput"/> only reports <see cref="KineticStatus.Playing"/> / <see cref="KineticStatus.Completed"/>.</summary>
        KineticStatus Status { get; }
    }
}
