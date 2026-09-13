namespace LordJEMO.Kinematics
{
    /// <summary>
    ///     One moving body in a group: its own <typeparamref name="TContext"/> (origin / target / …)
    ///     and its running <see cref="KineticPlayback"/>. The rule that drives it is the group's single
    ///     <c>Trajectory</c>. Blittable so a <see cref="Unity.Collections.NativeList{T}"/> of these
    ///     feeds <see cref="KineticEntryAdvanceJob{TTrajectory, TContext}"/>.
    /// </summary>
    public struct KineticEntry<TContext>
        where TContext : unmanaged, IKineticInput
    {
        /// <summary>Per-entry inputs the rule is evaluated against.</summary>
        public TContext Context;

        /// <summary>Per-entry pose / clock / status.</summary>
        public KineticPlayback State;
    }
}
