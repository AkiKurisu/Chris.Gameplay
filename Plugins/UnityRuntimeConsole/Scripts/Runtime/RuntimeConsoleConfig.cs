using System;
using Chris.Configs;

namespace UnityRuntimeConsole
{
    [Serializable]
    [ConfigPath("Chris.RuntimeConsole")]
    public class RuntimeConsoleConfig: Config<RuntimeConsoleConfig>
    {
        public bool enableConsoleInReleaseBuild;
    }
}