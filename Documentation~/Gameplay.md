# Gameplay

Based on Ceres to integrate visual scripting and C#. Also contains some tools that may be useful.

## Gameplay Architecture

Gameplay architecture is quite simple like Unreal.

### GameWorld

`GameWorld` is a gameplay level manager of scenes.

### Subsystem

`WorldSubsystem` is a singleton for each `GameWorld`.

### Actor

`Actor` is a gameplay level entity and is managed by `GameWorld`.

## Animation Proxy

`AnimationProxy` is a wrapper of `Animator` to play montage and sequence by script directly.

- Use `AnimationProxy` to play montage by `RuntimeAnimatorController` and `AnimationClip`.
- Support multi layers.
- Support subscribe events.
- Support visual scripting.

![AnimationProxy](./Images/animation_proxy.png)

### AnimationProxy Example

```C#
public class MontageExample : MonoBehaviour
{
    public Animator animator;
    private AnimationProxy animationProxy;
    public RuntimeAnimatorController controllerA;
    public RuntimeAnimatorController controllerB;
    private IEnumerator Start()
    {
        animationProxy = new AnimationProxy(animator);
        animationProxy.LoadAnimator(controllerA, 0.5f); /* Crossfade animator to controllerA in 0.5s */
        yield return new WaitForSeconds(1f);
        animationProxy.LoadAnimator(controllerB, 0.5f); /* Crossfade controllerA to controllerB in 0.5s */
        yield return new WaitForSeconds(1f);
        animationProxy.Stop(0.5f); /* Crossfade controllerB to animator in 0.5s */
    }
}
```

### SequenceBuilder Example

```C#
public class SequenceExample : MonoBehaviour
{
    public Animator animator;
    private AnimationProxy animationProxy;
    public AnimationClip[] clips;
    private void Start()
    {
        animationProxy = new AnimationProxy(animator);
        using var builder = animationProxy.CreateSequenceBuilder();
        foreach (var clip in clips)
        {
            builder.Append(clip, clip.length * 3 /* Play 3 loop */, 0.25f /* BlendIn duration */);
        }
        builder.SetBlendOut(0.5f);
        builder.Build().Run();
    }
}
```

### Debugging

Recommend to use [Unity PlayableGraph Monitor](`https://github.com/SolarianZ/UnityPlayableGraphMonitorTool`).

## Audios and FX

`AudioSystem` and `FXSystem` are designed to manage audio and fx within a scene using a general pooling method based on `Chris.Pool`.

- Support load from Addressables.
- Support visual scripting.
- Allocation optimization.

![FX Audios](./Images/fx_audio.png)

## Capture

The capture module provides a comprehensive screenshot and image capture system with support for both runtime and editor usage, visual scripting integration, and cross-platform gallery saving.

### Core Components

#### ScreenshotUtility

`ScreenshotUtility` is the core static class providing low-level screenshot capture functionality.

##### ScreenshotMode Enum

```C#
public enum ScreenshotMode
{
    /// <summary>
    /// Take raw screenshot from camera.
    /// </summary>
    Camera,
    /// <summary>
    /// Take screenshot from current screen color buffer.
    /// </summary>
    Screen
}
```

##### ScreenshotRequest Struct

```C#
public struct ScreenshotRequest
{
    /// <summary>
    /// Whether capture raw frame from renderer
    /// </summary>
    public ScreenshotMode Mode;
    
    /// <summary>
    /// Capture camera used when enable ScreenshotMode.Camera
    /// </summary>
    public Camera Camera;

    /// <summary>
    /// Define capture destination
    /// </summary>
    public RenderTexture Destination;
}
```

##### Core Methods

**CaptureScreenshot(ScreenshotRequest request)**
- Captures a screenshot based on the provided request configuration
- Returns the destination RenderTexture with captured content
- Supports both camera and screen capture modes

**ToTexture2D() Extension Methods**
- `ToTexture2D()`: Synchronously converts RenderTexture to Texture2D with HDR support
- `ToTexture2DAsync(Action<Texture2D> callback)`: Asynchronously converts RenderTexture using GPU readback

##### Usage Example

```C#
// Create a render texture for capture
var renderTexture = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);

// Configure capture request
var request = new ScreenshotRequest
{
    Mode = ScreenshotMode.Camera,
    Camera = Camera.main,
    Destination = renderTexture
};

// Capture screenshot
ScreenshotUtility.CaptureScreenshot(request);

// Convert to Texture2D
var texture2D = renderTexture.ToTexture2D();

// Clean up
RenderTexture.ReleaseTemporary(renderTexture);
```

#### ScreenshotTool

`ScreenshotTool` is a high-level component integrated with `Ceres.Flow` for visual scripting support and automated screenshot workflows.

##### Properties

- **SuperSize** (1-4): Resolution multiplier for higher quality captures
- **SourceCamera**: Camera used for capture (defaults to Camera.main)
- **ScreenshotMode**: Camera or Screen capture mode
- **EnableHDR**: Enable HDR rendering for captures

##### Executable Functions

**TakeScreenshot()**
- Takes a screenshot using current tool settings
- Automatically saves to gallery using GalleryUtility
- Triggers OnTakeScreenshotStart/End events

**GetLastScreenshot()**
- Returns the last captured Texture2D if available

##### Implementable Events

**OnTakeScreenshotStart()**
- Called before screenshot capture begins
- Override to customize pre-capture behavior

**OnTakeScreenshotEnd()**
- Called after screenshot capture completes
- Override to customize post-capture behavior

##### Usage Example

```C#
public class CustomScreenshotTool : ScreenshotTool
{
    public override void OnTakeScreenshotStart()
    {
        // Hide UI elements before capture
        UIManager.HideUI();
    }
    
    public override void OnTakeScreenshotEnd()
    {
        // Restore UI elements after capture
        UIManager.ShowUI();
    }
}
```

#### GalleryUtility

`GalleryUtility` provides cross-platform screenshot saving to device galleries and local folders.

##### Methods

**SavePngToGallery(byte[] byteArray)**
- Saves PNG data to gallery with auto-generated timestamp filename

**SavePngToGallery(string fileName, byte[] byteArray)**
- Saves PNG data to gallery with custom filename

##### Platform Behavior

| Platform | Behavior |
|----------|----------|
| Windows/Editor | Saves to `{ProjectRoot}/Snapshots/` folder |
| Android | Saves to device gallery with permission handling |
| iOS | Saves to device photo library |

##### Usage Example

```C#
// Capture and save screenshot
var texture = ScreenshotUtility.CaptureActiveRenderTexture(1920, 1080);
var pngData = texture.EncodeToPNG();
GalleryUtility.SavePngToGallery(pngData);
```

### Flow Graph Integration

The capture module provides four executable functions for visual scripting in `Ceres.Flow`:

#### Flow_CaptureRawScreenshot

```C#
[ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
public static Texture2D Flow_CaptureRawScreenshot(Camera camera, Vector2 size, 
    int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32)
```

Synchronously captures a raw screenshot from the specified camera with custom resolution and format settings.

#### Flow_CaptureRawScreenshotAsync

```C#
[ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
public static void Flow_CaptureRawScreenshotAsync(Camera camera, Vector2 size, 
    int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32, 
    Action<Texture2D> onComplete = null)
```

Asynchronously captures a raw screenshot with callback support for non-blocking operations.

#### Flow_CaptureScreenShotFromScreen

```C#
[ExecutableFunction, CeresLabel("Capture Screenshot"), CeresGroup("Gameplay/Capture")]
public static Texture2D Flow_CaptureScreenShotFromScreen()
```

Synchronously captures the current screen content. Must be called at end of frame.

#### Flow_CaptureScreenshotAsync

```C#
[ExecutableFunction, CeresLabel("Capture Screenshot Async"), CeresGroup("Gameplay/Capture")]
public static void Flow_CaptureScreenshotAsync(Action<Texture2D> onComplete)
```

Asynchronously captures the current screen content with callback support.

### Advanced Features

#### HDR Support
- Automatic HDR detection and gamma correction
- Support for ARGBHalf and ARGBFloat render texture formats
- Burst-compiled linear-to-gamma conversion for performance

#### Async GPU Readback
- Non-blocking texture conversion using AsyncGPUReadback
- UniTask integration for async/await patterns
- Automatic memory management and cleanup

#### Performance Optimization
- Temporary render texture pooling
- Burst-compiled color space conversion
- Efficient GPU-to-CPU data transfer

![ScreenshotTool](./Images/screenshot_tool.png)

## Actor Hotupdate

`Actor` is integrated with `Ceres.Flow` to support implementing logic in visual scripting.

Here is a simple example of how to implement a hotupdateable actor.

1. Create a `DataTable` named `ActorFlowGraphDataTable`.
2. Set `Row Type` to `Chris.Gameplay.ActorFlowGraphRow` and the dataTable will be automatically registered to Addressables.
3. Assign an address to `Advanced Settings/Actor Address` in your actor.

    ![Assign Address](./Images/assign_actor_address.png)

4. Create a new row in `ActorFlowGraphDataTable` and set `Row Id` to the address you assigned in step 3.

    ![Create Row](./Images/new_actor_row.png)

5. Enable `Remote Update` and overwrite remote update path if need.
6. Edit your actor flow graph and click `Save`.
7. Click `Download` button in `Advanced Settings` to export asset as an assetbundle.

    ![Export Asset](./Images/export_flow_asset.png)

    > [!TIP]
    > You can set `Remote Update/Serialize Mode` to `PreferText` in `Project Settings/Chris/Gameplay Settings` to export asset as a text file if has no asset dependencies for better readability.

8. Drag the bundle to `{Saved Path}/Flow` to update the actor.

### Saved Path

Saved path mentioned above is different for different platforms.

| Platform    | Path                            |
| ----------- | :-----------------------------: |
| Windows     | `Build Path`/Saved              |
| Android     | `Persistent Data Path`/Saved    |
