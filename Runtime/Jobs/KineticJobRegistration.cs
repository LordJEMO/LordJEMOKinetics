using Unity.Jobs;

// Generic Burst jobs must be registered once per concrete (trajectory, context) pair so the AOT
// compiler emits the specialisation. Add one line here for every IKineticTrajectory<TContext>
// implementation that needs the batched job path (KineticBatch / KineticUpdateJob<T, TContext>).
// A missing line surfaces as a runtime "job type not registered" error only in a player build, so
// keep this list in step with the Trajectories/ folder.
[assembly: RegisterGenericJobType(typeof(
    LordJEMO.Kinematics.KineticUpdateJob<LordJEMO.Kinematics.LinearTrajectory, LordJEMO.Kinematics.KineticInput>))]
[assembly: RegisterGenericJobType(typeof(
    LordJEMO.Kinematics.KineticEntryAdvanceJob<LordJEMO.Kinematics.LinearTrajectory, LordJEMO.Kinematics.KineticInput>))]
[assembly: RegisterGenericJobType(typeof(
    LordJEMO.Kinematics.KineticTransformWriteJob<LordJEMO.Kinematics.KineticInput>))]
[assembly: RegisterGenericJobType(typeof(
    LordJEMO.Kinematics.KineticEntryAdvanceJob<LordJEMO.Kinematics.ConstantVelocityTrajectory, LordJEMO.Kinematics.KineticVelocityInput>))]
[assembly: RegisterGenericJobType(typeof(
    LordJEMO.Kinematics.KineticTransformWriteJob<LordJEMO.Kinematics.KineticVelocityInput>))]
