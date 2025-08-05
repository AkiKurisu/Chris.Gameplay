using Chris.Configs;
using Chris.Serialization;
using R3.Chris;
using Newtonsoft.Json;
using R3;

namespace Chris.Graphics
{
    [PreferJsonConvert]
    [ConfigPath("Chris.Graphics")]
    public class GraphicsSettings: Config<GraphicsSettings>
    {
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> AmbientOcclusion { get; set; } = new(true);
        
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> Bloom { get; set; } = new(true);
        
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
#if UNITY_STANDALONE_WIN
        public ReactiveProperty<bool> DepthOfField { get; set; } = new(true);
#else
        public ReactiveProperty<bool> DepthOfField { get; set; } = new(false);
#endif
            
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> MotionBlur { get; set; } = new(false);
            
        [JsonConverter(typeof(ReactivePropertyConverter<int>))]
#if UNITY_STANDALONE_WIN
        public ReactiveProperty<int> RenderScale { get; set; } = new(4);
#else
        public ReactiveProperty<int> RenderScale { get; set; } = new(3);
#endif
        
        [JsonIgnore]
        public static readonly float[] RenderScalePresets = { 0.7f, 0.8f, 0.9f, 1.0f };
    
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> Tonemapping { get; set; } = new(true);
        
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> Vignette { get; set; } = new(true);
        
#if ILLUSION_RP_INSTALL
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
#if UNITY_STANDALONE_WIN
        public ReactiveProperty<bool> ContactShadows { get; set; } = new(true);
#else
        public ReactiveProperty<bool> ContactShadows { get; set; } = new(false);
#endif
            
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
#if UNITY_STANDALONE_WIN
        public ReactiveProperty<bool> ScreenSpaceReflection { get; set; } = new(true);
#else
        public ReactiveProperty<bool> ScreenSpaceReflection { get; set; } = new(false);
#endif
            
        [JsonConverter(typeof(ReactivePropertyConverter<bool>))]
        public ReactiveProperty<bool> VolumetricFog { get; set; } = new(true);
#endif
    }
}