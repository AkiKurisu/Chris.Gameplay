using System;
using Chris.Serialization;
using UnityEngine;

namespace Chris.Graphics
{
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Chris/Graphics/GraphicsConfig")]
    public class GraphicsConfig : ScriptableObject
    {
        [Serializable]
        public class CameraSettings
        {
            public float fieldOfView = 23;
            
            public float nearClipPlane = 0.1f;
            
            public float farClipPlane = 5000;

            public bool enableTextureStreaming = true;
            
            public bool perCameraStreaming;
        }

        public SerializedType<GraphicsModule>[] graphicsModules;

        public bool enableDepthOfField = true;

        public CameraSettings cameraSettings;
    }
}