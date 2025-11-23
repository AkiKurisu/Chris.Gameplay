using System.Collections;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Chris.Gameplay.Flow.Utilities;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Chris.Gameplay.Capture
{
    public class ScreenshotTool: FlowGraphObject
    {
        [Range(1, 4)]
        [SerializeField]
        private int superSize = 1;

        public int SuperSize
        {
            get => superSize;
            set => superSize = Mathf.Clamp(value, 1, 4);
        }

        [SerializeField]
        private Camera sourceCamera;

        public Camera SourceCamera
        {
            get => sourceCamera;
            set => sourceCamera = value;
        }

        [SerializeField] 
        private ScreenshotMode screenshotMode;
        
        public ScreenshotMode ScreenshotMode
        {
            get => screenshotMode;
            set => screenshotMode = value;
        }

        [SerializeField] 
        private bool enableHDR;
        
        public bool EnableHDR
        {
            get => enableHDR;
            set => enableHDR = value;
        }
        
        private Texture2D _captureTex;
        
#if UNITY_EDITOR
        [SerializeField] 
        private bool openFolderAfterCapture;
#endif
        
        private void OnDestroy()
        {
            DestroySafe(_captureTex);
        }
        
        private Camera GetCamera()
        {
            if (!sourceCamera) sourceCamera = Camera.main;
            return sourceCamera;
        }

        private IEnumerator TakeScreenshotCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            DestroySafe(_captureTex);

            // Can modify settings here
            OnTakeScreenshotStart();

            var screenSize = GameViewUtils.GetSizeOfMainGameView() * SuperSize;

            // Capture
            if (ScreenshotMode == ScreenshotMode.Screen)
            {
                GameplayExecutableLibrary.Flow_CaptureScreenshotAsync(ProcessPicture);
            }
            else
            {
                GameplayExecutableLibrary.Flow_CaptureRawScreenshotAsync(GetCamera(), screenSize,
                    renderTextureFormat: EnableHDR ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32, onComplete: ProcessPicture);
            }
        }

        private void ProcessPicture(Texture2D target)
        {
            // Encode
            _captureTex = target;
            var byteArray = target.EncodeToPNG();
            GalleryUtility.SavePngToGallery(byteArray);

            OnTakeScreenshotEnd();
            
#if UNITY_EDITOR
            if (openFolderAfterCapture && Application.isEditor)
            {
                System.Diagnostics.Process.Start(GalleryUtility.SnapshotFolderPath);
            }

            DestroySafe(_captureTex);
            _captureTex = null;
#endif
        }
        
        private static void DestroySafe(UObject uObject)
        {
            if (!uObject)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(uObject);
            }
            else
            {
                DestroyImmediate(uObject);
            }
        }
        
        /// <summary>
        /// Take a screenshot by tool current settings.
        /// </summary>
        [ExecutableFunction]
        public void TakeScreenshot()
        {
            StartCoroutine(TakeScreenshotCoroutine());
        }
        
        /// <summary>
        /// Get last screenshot if it exists.
        /// </summary>
        /// <returns></returns>
        [ExecutableFunction]
        public Texture2D GetLastScreenshot()
        {
            return _captureTex;
        }

        /// <summary>
        /// Process before taken screenshot
        /// </summary>
        [ImplementableEvent]
        public virtual void OnTakeScreenshotStart()
        {
            
        }
        
        /// <summary>
        /// Process after taken screenshot
        /// </summary>
        [ImplementableEvent]
        public virtual void OnTakeScreenshotEnd()
        {
            
        }
    }
}