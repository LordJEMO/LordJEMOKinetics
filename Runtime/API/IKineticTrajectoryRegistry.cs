using System;
using System.Collections.Generic;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Registers trajectories and hands out their emitters. Registration usually happens once, where the
    ///     manager is created; components that only emit look their emitter up with the same key.
    ///     Keys are <see cref="ITrajectoryData{TTrajectory, TInput}"/> objects compared by reference: one
    ///     group per key, even when two keys hold equal values. Trajectories are never removed.
    /// </summary>
    public interface IKineticTrajectoryRegistry
    {
        /// <summary>Number of registered trajectories.</summary>
        int TrajectoryCount { get; }

        /// <summary>Registers <paramref name="key"/>'s current trajectory; registering the same key again returns the same emitter.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> was registered with different type arguments.</exception>
        IKineticEmitter<TInput> Register<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;

        /// <summary>The emitter of a registered key.</summary>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not registered.</exception>
        IKineticEmitter<TInput> GetEmitter<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;

        /// <summary><c>false</c> (and a <c>null</c> emitter) when <paramref name="key"/> is not registered.</summary>
        bool TryGetEmitter<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key, out IKineticEmitter<TInput> emitter)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;

        bool IsRegistered<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>;
    }
}
