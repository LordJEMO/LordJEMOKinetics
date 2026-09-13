using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     The kinetic system: frame loop (<see cref="IKineticManager"/>), trajectory registry
    ///     (<see cref="IKineticTrajectoryRegistry"/>) and controlled editor (<see cref="IKineticTrajectoryEditor"/>)
    ///     over one group per registered key. The runtime backend is the injected
    ///     <see cref="IKineticTrajectoryGroupBuilder"/> (<see cref="KineticTrajectoryGroupMainBuilder"/> /
    ///     <see cref="KineticTrajectoryGroupJobsBuilder"/>).
    /// </summary>
    public sealed class KineticManager : IKineticManager, IKineticTrajectoryRegistry, IKineticTrajectoryEditor
    {
        private readonly IKineticTrajectoryGroupBuilder groupBuilder;

        private readonly List<IKineticTrajectoryGroup> groups = new List<IKineticTrajectoryGroup>();

        // key -> KineticEmitter<TTrajectory, TInput>. Stored as object because the emitter type differs per key.
        private readonly Dictionary<object, object> emitters = new Dictionary<object, object>(ReferenceKeyComparer.Instance);

        public KineticManager(IKineticTrajectoryGroupBuilder groupBuilder)
        {
            if (groupBuilder == null)
            {
                throw new ArgumentNullException(nameof(groupBuilder));
            }

            this.groupBuilder = groupBuilder;
        }

        public int TrajectoryCount => groups.Count;

        public int Count
        {
            get
            {
                int total = 0;
                for (int i = 0; i < groups.Count; i++)
                {
                    total += groups[i].Count;
                }

                return total;
            }
        }

        #region IKineticTrajectoryRegistry Implementation

        public IKineticEmitter<TInput> Register<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (emitters.TryGetValue(key, out object existing))
            {
                return Cast<TTrajectory, TInput>(existing, key);
            }

            IKineticTrajectoryGroup<TTrajectory, TInput> group = groupBuilder.Build<TTrajectory, TInput>(key.Trajectory);
            groups.Add(group);

            KineticEmitter<TTrajectory, TInput> emitter = new KineticEmitter<TTrajectory, TInput>(group);
            emitters.Add(key, emitter);
            return emitter;
        }

        public IKineticEmitter<TInput> GetEmitter<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            return Resolve(key);
        }

        public bool TryGetEmitter<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key, out IKineticEmitter<TInput> emitter)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (emitters.TryGetValue(key, out object existing))
            {
                emitter = Cast<TTrajectory, TInput>(existing, key);
                return true;
            }

            emitter = null;
            return false;
        }

        public bool IsRegistered<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return emitters.ContainsKey(key);
        }

        #endregion

        #region IKineticTrajectoryEditor Implementation

        public void Refresh<TTrajectory, TInput>(ITrajectoryAssetData<TTrajectory, TInput> asset)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            Resolve(asset).Group.SetTrajectory(asset.Trajectory);
        }

        public void Update<TTrajectory, TInput>(KineticTrajectoryKey<TTrajectory, TInput> key, in TTrajectory trajectory)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            KineticEmitter<TTrajectory, TInput> emitter = Resolve(key);
            key.Trajectory = trajectory;
            emitter.Group.SetTrajectory(in trajectory);
        }

        #endregion

        #region IKineticManager Implementation

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Tick(deltaTime);
            }
        }

        public void ApplyTransforms()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].ApplyTransforms();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Dispose();
            }

            groups.Clear();
            emitters.Clear();
        }

        #endregion

        private KineticEmitter<TTrajectory, TInput> Resolve<TTrajectory, TInput>(ITrajectoryData<TTrajectory, TInput> key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!emitters.TryGetValue(key, out object existing))
            {
                throw new KeyNotFoundException($"This {key.GetType().Name} key is not registered in the kinetic manager.");
            }

            return Cast<TTrajectory, TInput>(existing, key);
        }

        private static KineticEmitter<TTrajectory, TInput> Cast<TTrajectory, TInput>(object existing, object key)
            where TInput : unmanaged, IKineticInput
            where TTrajectory : unmanaged, IKineticTrajectory<TInput>
        {
            if (existing is KineticEmitter<TTrajectory, TInput> typed)
            {
                return typed;
            }

            throw new ArgumentException($"This {key.GetType().Name} key is registered as a different trajectory than {typeof(TTrajectory).Name} over {typeof(TInput).Name}.");
        }

        // Keys are matched by identity even if a key type overrides Equals; .NET Standard 2.1 has no ReferenceEqualityComparer.
        private sealed class ReferenceKeyComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceKeyComparer Instance = new ReferenceKeyComparer();

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
