using Ceres.Annotations;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Gameplay.Audios;
using Chris.Gameplay.Capture;
using Chris.Gameplay.FX;
using Chris.Gameplay.Level;
using Chris.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Chris.Gameplay.Flow.Utilities
{
    /// <summary>
    /// Executable function library for Gameplay
    /// </summary>
    [CeresGroup("Gameplay")]
    public partial class GameplayExecutableLibrary: ExecutableFunctionLibrary
    {
        #region Subsystem

        [ExecutableFunction]
        public static SubsystemBase Flow_GetSubsystem(
            [ResolveReturn] SerializedType<SubsystemBase> type)
        {
            return GameWorld.Get().GetSubsystem(type);
        }
        
        [ExecutableFunction]
        public static SubsystemBase Flow_GetOrCreateSubsystem(
            [ResolveReturn] SerializedType<SubsystemBase> type)
        {
            return WorldSubsystem.GetOrCreate(type);
        }
        
        #endregion

        #region Audios

        [ExecutableFunction, CeresLabel("Play 2D AudioClip"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Play2DAudioClip(AudioClip audioClip, float volume = 1f)
        {
            AudioSystem.PlayClipAtPoint(audioClip, default, volume, 0);
        }
        
        [ExecutableFunction, CeresLabel("Play 2D AudioClip from Address"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Play2DAudioClipFromAddress(string audioClipAddress, float volume = 1f)
        {
            AudioSystem.PlayClipAtPoint(audioClipAddress, default, volume, 0);
        }
        
        [ExecutableFunction, CeresLabel("Play 3D AudioClip"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Play3DAudioClip(AudioClip audioClip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.PlayClipAtPoint(audioClip, position, volume, spatialBlend, minDistance);
        }
        
        [ExecutableFunction, CeresLabel("Play 3D AudioClip from Address"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Play3DAudioClipFromAddress(string audioClipAddress, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.PlayClipAtPoint(audioClipAddress, position, volume, spatialBlend, minDistance);
        }
        
        [ExecutableFunction, CeresLabel("Schedule 3D AudioClip"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Schedule3DAudioClip(AudioClip audioClip, Vector3 position, float scheduleTime, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.ScheduleClipAtPoint(audioClip, position, scheduleTime, volume, spatialBlend, minDistance);
        }
        
        [ExecutableFunction, CeresLabel("Schedule 3D AudioClip from Address"), CeresGroup("Gameplay/Audios")]
        public static void Flow_Schedule3DAudioClipFromAddress(string audioClipAddress, Vector3 position, float scheduleTime, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.ScheduleClipAtPoint(audioClipAddress, position, scheduleTime, volume, spatialBlend, minDistance);
        }

        [ExecutableFunction, CeresLabel("Stop AudioClip"), CeresGroup("Gameplay/Audios")]
        public static void Flow_StopAudioClip(AudioClip audioClip)
        {
            AudioSystem.StopAudioClip(audioClip);
        }
        
        [ExecutableFunction, CeresLabel("Stop AudioClip from Address"), CeresGroup("Gameplay/Audios")]
        public static void Flow_StopAudioClip(string audioClipAddress)
        {
            AudioSystem.StopAudioClip(audioClipAddress);
        }

        #endregion

        #region FX
        
        [ExecutableFunction, CeresLabel("Play ParticleSystem"), CeresGroup("Gameplay/FX")]
        public static void Flow_PlayParticleSystem(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool useLocalPosition = false)
        {
            FXSystem.PlayFX(prefab, position, rotation, parent, useLocalPosition);
        }

        [ExecutableFunction, CeresLabel("Play ParticleSystem from Address"), CeresGroup("Gameplay/FX")]
        public static void Flow_PlayParticleSystemFromAddress(string prefabAddress, Vector3 position, Quaternion rotation, Transform parent = null, bool useLocalPosition = false)
        {
            FXSystem.PlayFX(prefabAddress, position, rotation, parent, useLocalPosition);
        }
        
        #endregion

        #region Level

        [ExecutableFunction, CeresLabel("Load Level"), CeresGroup("Gameplay/Level")]
        public static void Flow_LoadLevel(LevelReference levelReference)
        {
            LevelSystem.LoadAsync(levelReference).Forget();
        }
        
        [ExecutableFunction, CeresLabel("Load Level from Name"), CeresGroup("Gameplay/Level")]
        public static void Flow_LoadLevelFromName(string levelName)
        {
            LevelSystem.LoadAsync(levelName).Forget();
        }
        
        [ExecutableFunction, CeresLabel("Find Level from Name"), CeresGroup("Gameplay/Level")]
        public static LevelReference Flow_FindLevel(string levelName)
        {
            return LevelSceneDataTableManager.Get().FindLevel(levelName);
        }
        
        [ExecutableFunction, CeresLabel("Find Level from Tag"), CeresGroup("Gameplay/Level")]
        public static LevelReference Flow_FindLevelFromTag(string tag)
        {
            return LevelSceneDataTableManager.Get().FindLevelFromTag(tag);
        }

        #endregion
        
        #region Capture
        
        [ExecutableFunction, CeresLabel("Capture Screen Shot to Texture2D"), CeresGroup("Gameplay/Capture")]
        public static Texture2D CaptureScreenShotToTexture2D(Camera camera, Vector2 size)
        {
            var screenTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, 
                0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            ScreenShotUtils.CaptureScreenShot(new ScreenShotRequest
            {
                Camera = camera,
                Destination = screenTexture,
                FromRenderer = true
            }); 
            var captureTex = screenTexture.ToTexture2D();
            RenderTexture.ReleaseTemporary(screenTexture);
            return captureTex;
        }
        
        #endregion Capture
    }
}
