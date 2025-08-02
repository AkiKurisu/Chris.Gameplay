using System;
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
using UnityEngine.Assertions;

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

        #region Actor
        
        [ExecutableFunction]
        public static Actor Flow_GetActor(ActorHandle handle)
        {
            return GameWorld.Get().GetActor(handle);
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
        
        /// <summary>
        /// Capture raw screenshot from renderer to a new <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="size"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="renderTextureFormat"></param>
        /// <returns></returns>
        [ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
        public static Texture2D Flow_CaptureRawScreenshot(Camera camera, Vector2 size, 
            int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32)
        {
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, 
                depthBuffer, renderTextureFormat, RenderTextureReadWrite.Default, antiAliasing);
            ScreenshotUtility.CaptureScreenshot(new ScreenshotRequest
            {
                Camera = camera,
                Destination = screenTexture,
                Mode = ScreenshotMode.Camera
            }); 
            var captureTex = screenTexture.ToTexture2D();
            RenderTexture.ReleaseTemporary(screenTexture);
            return captureTex;
        }

        /// <summary>
        /// Capture raw screenshot from renderer to a new <see cref="Texture2D"/> in async.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="size"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="renderTextureFormat"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        [ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
        public static void Flow_CaptureRawScreenshotAsync(Camera camera, Vector2 size, 
            int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32, Action<Texture2D> onComplete = null)
        {
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, 
                depthBuffer, renderTextureFormat, RenderTextureReadWrite.Default, antiAliasing);
            ScreenshotUtility.CaptureScreenshot(new ScreenshotRequest
            {
                Camera = camera,
                Destination = screenTexture,
                Mode = ScreenshotMode.Camera
            }); 
            
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                var result = screenTexture.ToTexture2D();
                onComplete?.Invoke(result);
                RenderTexture.ReleaseTemporary(screenTexture);
                return;
            }
#endif
            screenTexture.ToTexture2DAsync(result =>
            {
                onComplete?.Invoke(result);
                RenderTexture.ReleaseTemporary(screenTexture);
            });
        }
        
        /// <summary>
        /// Capture screenshot to a new <see cref="Texture2D"/>. Need be called at the end of frame.
        /// </summary>
        /// <returns></returns>
        [ExecutableFunction, CeresLabel("Capture Screenshot"), CeresGroup("Gameplay/Capture")]
        public static Texture2D Flow_CaptureScreenShotFromScreen()
        {
            var screenSize = GameView.GetSizeOfMainGameView();
#if UNITY_EDITOR
            return ScreenshotUtility.CaptureActiveRenderTexture((int)screenSize.x, (int)screenSize.y);
#else
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, antiAliasing);
            ScreenshotUtility.CaptureScreenshot(new ScreenshotRequest
            {
                Destination = screenTexture,
                Mode = ScreenshotMode.Screen
            }); 
            var captureTex = screenTexture.ToTexture2D();
            RenderTexture.ReleaseTemporary(screenTexture);
            return captureTex;
#endif
        }
        
        /// <summary>
        /// Capture screenshot to a new <see cref="Texture2D"/> in async. Need be called at the end of frame.
        /// </summary>
        /// <param name="onComplete"></param>
        [ExecutableFunction, CeresLabel("Capture Screenshot Async"), CeresGroup("Gameplay/Capture")]
        public static void Flow_CaptureScreenshotAsync(Action<Texture2D> onComplete)
        {
            Assert.IsNotNull(onComplete);
            var screenSize = GameView.GetSizeOfMainGameView();
#if UNITY_EDITOR
            onComplete(ScreenshotUtility.CaptureActiveRenderTexture((int)screenSize.x, (int)screenSize.y));
#else
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, antiAliasing);
            ScreenshotUtility.CaptureScreenshot(new ScreenshotRequest
            {
                Destination = screenTexture,
                Mode = ScreenshotMode.Screen
            }); 
            screenTexture.ToTexture2DAsync(result =>
            {
                onComplete.Invoke(result);
                RenderTexture.ReleaseTemporary(screenTexture);
            });
#endif
        }
        
        #endregion Capture
    }
}
