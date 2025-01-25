using Ceres.Graph;
using Chris.Gameplay.Animations;
using UnityEngine;
namespace Chris.Gameplay.Flow.Utilities
{
    internal static class GameplaySetup
    {
        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void InitializeOnLoad()
        {
            /* Register port implicit conversation */
            // ========================= Animation =========================== //
            CeresPort<LayerHandle>.MakeCompatibleTo<int>(handle => handle.Id);
            CeresPort<int>.MakeCompatibleTo<LayerHandle>(d => new LayerHandle(d));
            CeresPort<string>.MakeCompatibleTo<LayerHandle>(str => new LayerHandle(str));
            // ========================= Animation =========================== //
        }
    }
}