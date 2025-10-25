using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using R3;
using UnityEngine.Rendering.Universal;
using Chris.Gameplay;
using System.Linq;
#if ILLUSION_RP_INSTALL
using Illusion.Rendering;
#endif

namespace Chris.Graphics
{
    /// <summary>
    /// Runtime graphics controller
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [ExecuteInEditMode]
    public class GraphicsController : MonoBehaviour
    {
        private readonly Dictionary<DynamicVolumeType, Volume> _volumes = new();

        public GraphicsConfig graphicsConfig;
        
        private GraphicsModule[] _graphicsModules;
        
        private GraphicsSettings _settings;
        
        private UniversalRenderPipelineAsset _urpAsset;
        
        internal const string LookDevModeKey = "Graphics.LookDevMode";
        
        /// <summary>
        /// Get world instance graphics controller if exist
        /// </summary>
        /// <returns></returns>
        public static GraphicsController Get()
        {
            return ContainerSubsystem.Get().Resolve<GraphicsController>();
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid()) return;
#endif
            
            PrepareDynamicVolumes();

            ApplyCameraSettings();
        }

        private void Start()
        {
            InitializeGraphics();
        }

        private void InitializeGraphics()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid()) return;
#endif
            _settings = GraphicsSettings.Get();
            _urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            
            // Apply dynamic volume if config is set
            if (graphicsConfig)
            {
                ApplyDynamicVolumeProfiles();
                SetDepthOfFieldEnabled(Application.isPlaying && graphicsConfig.enableDepthOfField);
            }
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Reset urp asset modification in editor
                _urpAsset.renderScale = 1;
                return;
            }
#endif
            
            ContainerSubsystem.Get().Register(this);

            // Bind properties
            var d = Disposable.CreateBuilder();
            _settings.AmbientOcclusion.Subscribe(SetAmbientOcclusionEnabled).AddTo(ref d);
            _settings.Bloom.Subscribe(SetBloomEnabled).AddTo(ref d);
            if (graphicsConfig.enableDepthOfField)
            {
                _settings.DepthOfField.Subscribe(SetDepthOfFieldEnabled).AddTo(ref d);
            }
            _settings.MotionBlur.Subscribe(SetMotionBlurEnabled).AddTo(ref d);
            _settings.Vignette.Subscribe(SetVignetteEnabled).AddTo(ref d);
            _settings.RenderScale.Subscribe(SetRenderScale).AddTo(ref d);
            _settings.FrameRate.Subscribe(SetFrameRate).AddTo(ref d);
#if ILLUSION_RP_INSTALL
            _settings.ContactShadows.Subscribe(SetContactShadowsEnabled).AddTo(ref d);
            _settings.ScreenSpaceReflection.Subscribe(SetScreenSpaceReflection).AddTo(ref d);
            _settings.ScreenSpaceGlobalIllumination.Subscribe(SetScreenSpaceGlobalIllumination).AddTo(ref d);
            _settings.VolumetricFog.Subscribe(SetVolumetricFog).AddTo(ref d);
#endif
            _graphicsModules = graphicsConfig.graphicsModules.Select(serializedType => serializedType.GetObject()).ToArray();
            d.Build().AddTo(this);
            
            // Initialize sub-modules
            foreach (var module in _graphicsModules)
            {
                module?.Initialize(this, _settings);
            }
        }

        private void SetFrameRate(int index)
        {
            if (!Application.isPlaying) return;
            if (index >= graphicsConfig.frameRateOptions.Length) return;
            
            Application.targetFrameRate = graphicsConfig.frameRateOptions[index];
        }

        public void ApplyCameraSettings()
        {
            var mainCamera = Camera.main;
            if (!mainCamera) return;

            mainCamera.fieldOfView = graphicsConfig.fieldOfView;
            mainCamera.nearClipPlane = graphicsConfig.nearClipPlane;
            mainCamera.farClipPlane = graphicsConfig.farClipPlane;
            QualitySettings.streamingMipmapsActive = graphicsConfig.enableTextureStreaming;
            if (graphicsConfig.perCameraStreaming)
            {
                _ = mainCamera.gameObject.AddComponent<StreamingController>();
            }
        }

        private void PrepareDynamicVolumes()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
#endif
            foreach (var volumeType in (DynamicVolumeType[])Enum.GetValues(typeof(DynamicVolumeType)))
            {
                var newObject = new GameObject(volumeType.ToString())
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                newObject.transform.SetParent(transform);
                var volume = newObject.AddComponent<Volume>();
                _volumes.Add(volumeType, volume);
            }
        }

        public void ApplyVolumeProfiles(DynamicVolumePlatform? overridePlatform = null)
        {
#if UNITY_EDITOR
            bool lookDevMode = UnityEditor.EditorPrefs.GetBool(LookDevModeKey, false);
#endif
            var manager = DynamicVolumeProfileTableManager.Get();
            foreach (var volumeType in (DynamicVolumeType[])Enum.GetValues(typeof(DynamicVolumeType)))
            {
                ApplyVolumeProfile(GetVolume(volumeType), manager.GetProfile(volumeType, overridePlatform), manager.GetPriority(volumeType));
#if UNITY_EDITOR
                if (lookDevMode && IsLookDevVolumeType(volumeType))
                {
                    GetVolume(volumeType).enabled = false;
                }
#endif
            }
        }
        
        public void ApplyDynamicVolumeProfiles()
        {
            ApplyVolumeProfiles();
        }

        private static void ApplyVolumeProfile(Volume volume, VolumeProfile profile, float priority)
        {
            if (volume.profile != profile)
            {
                volume.profile = profile;
            }

            volume.priority = priority;
        }
        
        public ScriptableRendererData GetUniversalScriptableRendererData()
        {
            return UniversalRenderingUtility.GetDefaultRendererData(_urpAsset);
        }

        internal static bool IsLookDevVolumeType(DynamicVolumeType volumeType)
        {
#if ILLUSION_RP_INSTALL
            if (volumeType == DynamicVolumeType.SubsurfaceScattering) return true;
#endif
            return volumeType == DynamicVolumeType.Tonemapping;
        }
        
        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid()) return;
            if (!Application.isPlaying) return;
#endif
            if (_graphicsModules != null)
            {
                foreach (var module in _graphicsModules)
                {
                    module?.Dispose();
                }
            }
            _settings?.Save();
            ContainerSubsystem.Get().Unregister(this);
        }

        private void SetRenderScale(int presetId /* Index + 1 */)
        {
            if (presetId >= 1 && presetId <= GraphicsSettings.RenderScalePresets.Length)
            {
                _urpAsset.renderScale = GraphicsSettings.RenderScalePresets[presetId - 1];
            }
        }

        public Volume GetVolume(DynamicVolumeType dynamicVolumeType)
        {
            if (!_volumes.Any())
            {
                PrepareDynamicVolumes();
            }
            return _volumes[dynamicVolumeType];
        }

        [Conditional("UNITY_EDITOR")]
        internal void InitializeIfNeed()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid()) return;
#endif
            if (!_volumes.Any())
            {
                PrepareDynamicVolumes();
                InitializeGraphics();
            }
        }

        private void SetDepthOfFieldEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.DepthOfField).weight = isEnabled ? 1f : 0f;
        }
        
        private void SetMotionBlurEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.MotionBlur).weight = isEnabled ? 1f : 0f;
        }
        
        private void SetAmbientOcclusionEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.AmbientOcclusion).weight = isEnabled ? 1f : 0f;
        }
        
        private void SetVignetteEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.Vignette).weight = isEnabled ? 1f : 0f;
        }
        
        private void SetBloomEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.Bloom).weight = isEnabled ? 1f : 0f;
        }
        
#if ILLUSION_RP_INSTALL
        private void SetContactShadowsEnabled(bool isEnabled)
        {
            GetVolume(DynamicVolumeType.ContactShadows).weight = isEnabled ? 1f : 0f;
        }
        
        private static void SetScreenSpaceReflection(bool isEnabled)
        {
            IllusionRuntimeRenderingConfig.Get().EnableScreenSpaceReflection = isEnabled;
        }
        
        private static void SetScreenSpaceGlobalIllumination(bool isEnabled)
        {
            IllusionRuntimeRenderingConfig.Get().EnableScreenSpaceGlobalIllumination = isEnabled;
        }
        
        private static void SetVolumetricFog(bool isEnabled)
        {
            IllusionRuntimeRenderingConfig.Get().EnableVolumetricFog = isEnabled;
        }
#endif
    }
}
