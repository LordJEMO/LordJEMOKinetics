namespace LordJEMO.Kinematics
{
    /// <summary>Which <see cref="IKineticManager"/> backend to build.</summary>
    public enum KineticExecutionMode
    {
        /// <summary>Single-threaded, no Jobs/Burst. Safe on WebGL and stripped runtimes.</summary>
        Main = 0,

        /// <summary>Burst <c>IJobFor</c> advance + <c>IJobParallelForTransform</c> transform write.</summary>
        Jobs = 1,
    }
}
