using System;
using Chris.Serialization;
using UnityEngine;

namespace Chris.Gameplay.Graphics
{
    [Flags]
    public enum GraphicsFeatures
    {
        None = 0,
        ScreenSpaceReflection = 1 << 0,
        ScreenSpaceGlobalIllumination = 1 << 1,
        ScreenSpaceAmbientOcclusion = 1 << 2,
        DepthOfField = 1 << 3,
        MotionBlur = 1 << 4
    }
    
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Chris/Graphics/GraphicsConfig")]
    public class GraphicsConfig : ScriptableObject
    {
        // Camera Settings
        [Header("Camera Settings")]
        public float fieldOfView = 23;
            
        public float nearClipPlane = 0.1f;
            
        public float farClipPlane = 5000;

        // Quality Settings
        [Header("Quality Settings")]
        public int[] frameRateOptions = { 30, 60 };

        public bool enableTextureStreaming = true;
            
        public bool perCameraStreaming;

        // Extra Settings
        [Header("Extra Settings")]
        public SerializedType<GraphicsModule>[] graphicsModules;

        [SerializeField]
        private GraphicsFeatures disableFeatures;

        public bool IsFeatureSupport(GraphicsFeatures features)
        {
            return (disableFeatures & features) == 0;
        }
        
        public bool IsVolumeSupport(DynamicVolumeType dynamicVolumeType)
        {
            if (dynamicVolumeType == DynamicVolumeType.DepthOfField)
            {
                return !disableFeatures.HasFlag(GraphicsFeatures.DepthOfField) && Application.isPlaying;
            }
            
            if (dynamicVolumeType == DynamicVolumeType.MotionBlur)
            {
                return !disableFeatures.HasFlag(GraphicsFeatures.MotionBlur) && Application.isPlaying;
            }
            
            if (dynamicVolumeType == DynamicVolumeType.ScreenSpaceReflection)
            {
                return !disableFeatures.HasFlag(GraphicsFeatures.ScreenSpaceReflection);
            }
            
            if (dynamicVolumeType == DynamicVolumeType.ScreenSpaceGlobalIllumination)
            {
                return !disableFeatures.HasFlag(GraphicsFeatures.ScreenSpaceGlobalIllumination);
            }
            
            if (dynamicVolumeType == DynamicVolumeType.ScreenSpaceAmbientOcclusion)
            {
                return !disableFeatures.HasFlag(GraphicsFeatures.ScreenSpaceAmbientOcclusion);
            }

            return true;
        }
    }
}