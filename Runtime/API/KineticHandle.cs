using System;

namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     Opaque reference to one entry (a moving body) inside an
    ///     <see cref="IKineticTrajectoryGroup"/>. Frame-stable and safe to hold across removals of
    ///     other entries: <see cref="Id"/> is a stable slot id, <see cref="Version"/> is bumped when
    ///     that id is recycled so a stale handle is detected rather than silently reused.
    ///
    ///     A handle is only meaningful for the group that issued it — the group is the context (that
    ///     is why there is no type-id field). The <see cref="KineticObject"/> wrapper binds a handle
    ///     to its group so normal call sites cannot mix them up.
    /// </summary>
    public readonly struct KineticHandle : IEquatable<KineticHandle>
    {
        /// <summary>An invalid handle.</summary>
        public static readonly KineticHandle None = new KineticHandle(-1, 0);

        /// <summary>Stable slot id inside the issuing group (survives swap-removes of other entries).</summary>
        public readonly int Id;

        /// <summary>Recycle counter for <see cref="Id"/>; a mismatch means the handle is stale.</summary>
        public readonly int Version;

        public KineticHandle(int id, int version)
        {
            Id = id;
            Version = version;
        }

        public bool Equals(KineticHandle other)
        {
            return Id == other.Id && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is KineticHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ Version;
            }
        }

        public static bool operator ==(KineticHandle a, KineticHandle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(KineticHandle a, KineticHandle b)
        {
            return !a.Equals(b);
        }
    }
}
