using System.IO;
using UnityEngine;

namespace Chris.Gameplay.Mod
{
    public static class ImportConstants
    {
        internal const string DefaultAPIVersion = "0.1.0";

        public const string DynamicLoadPath = "{LOCAL_MOD_PATH}";

#if !UNITY_EDITOR && UNITY_ANDROID
        public static string LoadingPath = Path.Combine(Application.persistentDataPath, "Mods");
#else
        public static string LoadingPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Mods");
#endif
    }
}