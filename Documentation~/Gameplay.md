# Gameplay

Based on Ceres to integrate visual scripting and C#. Also contains some tools that may be useful.

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