using Chris.Serialization;
using UnityEngine;

namespace Chris.Graphics
{
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Chris/Graphics/GraphicsConfig")]
    public class GraphicsConfig : ScriptableObject
    {
        // Camera Settings
        [Header("Camera Settings")]
        public float fieldOfView = 23;
            
        public float nearClipPlane = 0.1f;
            
        public float farClipPlane = 5000;

        public bool enableDepthOfField = true;

        // Quality Settings
        [Header("Quality Settings")]
        public int[] frameRateOptions = { 30, 60 };

        public bool enableTextureStreaming = true;
            
        public bool perCameraStreaming;

        // Extra Settings
        [Header("Extra Settings")]
        public SerializedType<GraphicsModule>[] graphicsModules;
    }
}