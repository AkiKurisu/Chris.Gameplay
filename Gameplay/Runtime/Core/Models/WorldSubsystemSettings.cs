using System;
using Chris.Configs;

namespace Chris.Gameplay
{
    /// <summary>
    /// Settings for <see cref="WorldSubsystem"/>
    /// </summary>
    [Serializable]
    [ConfigPath("Chris.Gameplay.WorldSubsystem")]
    public class WorldSubsystemSettings: Config<WorldSubsystemSettings>
    {
        /// <summary>
        /// Whether to ensure that world subsystem is initialized before getting the system instance.
        /// </summary>
        public bool subsystemForceInitializeBeforeGet = true;
    }
}