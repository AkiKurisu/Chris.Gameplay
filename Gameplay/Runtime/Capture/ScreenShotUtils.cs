using UnityEngine;
using UnityEngine.Assertions;

namespace Chris.Gameplay.Capture
{
    public struct ScreenShotRequest
    {
        /// <summary>
        /// Whether capture raw frame from renderer
        /// </summary>
        public bool FromRenderer;
        
        /// <summary>
        /// Capture camera used when enable <see cref="FromRenderer"/>
        /// </summary>
        public Camera Camera;

        /// <summary>
        /// Define capture destination
        /// </summary>
        public RenderTexture Destination;
    }
    
    public static class ScreenShotUtils
    {
        private static void CaptureScreenShotFromRenderer(RenderTexture renderTarget, Camera camera)
        {
            camera.targetTexture = renderTarget;
            camera.Render();
            camera.targetTexture = null;
        }
        
        private static void CaptureScreenShot(RenderTexture renderTarget)
        {
            var screenSize = GameView.GetSizeOfMainGameView();
            RenderTexture cameraTarget = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                0, RenderTextureFormat.ARGB32);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(cameraTarget);
            Graphics.Blit(cameraTarget, renderTarget, new Vector2(1f, -1f), new Vector2(0.0f, 1f)); // Flip in DX12 and Vulkan
            RenderTexture.ReleaseTemporary(cameraTarget);
        }

        public static RenderTexture CaptureScreenShot(ScreenShotRequest request)
        {
            var destination = request.Destination;
            Assert.IsTrue((bool)destination);
            if (request.FromRenderer)
            {
                CaptureScreenShotFromRenderer(destination, request.Camera);
            }
            else
            {
                CaptureScreenShot(destination);
            }

            return destination;
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture)
        {
            RenderTexture original = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var destination = new Texture2D(renderTexture.width, renderTexture.height);
            destination.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            RenderTexture.active = original;
            return destination;
        }
    }
}