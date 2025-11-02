using System;
using System.Collections.Generic;
using Chris.DataDriven;
using Chris.Pool;
using Chris.Resource;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chris.Gameplay.Graphics
{
    /// <summary>
    /// Define volume type that can can be altered dynamically
    /// </summary>
    public enum DynamicVolumeType
    {
        Bloom,
        DepthOfField,
        MotionBlur,
        Tonemapping,
        Vignette,
        ScreenSpaceAmbientOcclusion,
        ScreenSpaceReflection,
        ScreenSpaceGlobalIllumination,
        SubsurfaceScattering,
        PercentageCloserSoftShadows,
        ContactShadows,
        VolumetricFog
    }

    /// <summary>
    /// Define platform options for Dynamic Volume
    /// </summary>
    public enum DynamicVolumePlatform
    {
        Windows,
        Mobile,
        Console
    }
    
    [Serializable, AddressableDataTable(address: DynamicVolumeProfileTableManager.TableKey)]
    public class DynamicVolumeProfileRow: IDataTableRow
    {
        public DynamicVolumeType type;
        
        [AssetReferenceConstraint(group: "Volumes")]
        public SoftAssetReference<VolumeProfile> windowsProfile;
        
        [AssetReferenceConstraint(group: "Volumes")]
        public SoftAssetReference<VolumeProfile> mobileProfile;
                
        [AssetReferenceConstraint(group: "Volumes")]
        public SoftAssetReference<VolumeProfile> consoleProfile;

        [Tooltip("A value which determines which Volume is being used when Volumes have an equal amount of influence on the Scene. " +
                 "Volumes with a higher priority will override lower ones.")]
        public int priority;

        public SoftAssetReference<VolumeProfile> GetVolumeProfile(DynamicVolumePlatform? overridePlatform = null)
        {
            DynamicVolumePlatform platform;

            if (overridePlatform.HasValue)
            {
                platform = overridePlatform.Value;
            }
            else
            {
#if UNITY_ANDROID || UNITY_IOS
                platform = DynamicVolumePlatform.Mobile;
#elif UNITY_XBOXONE || UNITY_PS5
                platform = DynamicVolumePlatform.Console;
#else
                platform = DynamicVolumePlatform.Windows;
#endif
            }

            SoftAssetReference<VolumeProfile> profileReference = windowsProfile;
            switch (platform)
            {
                case DynamicVolumePlatform.Windows:
                    profileReference = windowsProfile;
                    break;
                case DynamicVolumePlatform.Mobile:
                    profileReference = mobileProfile;
                    break;
                case DynamicVolumePlatform.Console:
                    profileReference = consoleProfile;
                    break;
            }

            if (profileReference == null || string.IsNullOrEmpty(profileReference.Address))
            {
                profileReference = windowsProfile;
            }

            return profileReference;
        }
    }
    
    public class DynamicVolumeProfileTableManager : DataTableManager<DynamicVolumeProfileTableManager>
    {
        public DynamicVolumeProfileTableManager(object _) : base(_)
        {
        }

        public const string TableKey = "DynamicVolumeProfileTable";

        private readonly Dictionary<DynamicVolumeType, DynamicVolumeProfileRow> _profileRows = new();
        
        protected sealed override async UniTask Initialize(bool sync)
        {
            await InitializeSingleTable(TableKey, sync);
            var dt = GetDataTable(TableKey);
            var rows = dt.GetAllRows<DynamicVolumeProfileRow>();
            if (sync)
            {
                foreach (var row in rows)
                {
                    _profileRows[row.type] = row;
#if UNITY_ANDROID || UNITY_IOS
                    row.mobileProfile.LoadAsync().WaitForCompletion();
#elif UNITY_XBOXONE || UNITY_PS5
                    row.consoleProfile.LoadAsync().WaitForCompletion();
#else
                    row.windowsProfile.LoadAsync().WaitForCompletion();
#endif
                }
                return;
            }
            
            using var parallel = UniParallel.Get();
            foreach (var row in rows)
            {
                _profileRows[row.type] = row;
#if UNITY_ANDROID || UNITY_IOS
                parallel.Add(row.mobileProfile.LoadAsync().ToUniTask());
#elif UNITY_XBOXONE || UNITY_PS5
                parallel.Add(row.consoleProfile.LoadAsync().ToUniTask());
#else
                parallel.Add(row.windowsProfile.LoadAsync().ToUniTask());
#endif
            }
            await parallel;
        }

        public VolumeProfile GetProfile(DynamicVolumeType volumeType, DynamicVolumePlatform? overridePlatform = null)
        {
            if (_profileRows.TryGetValue(volumeType, out var row))
            {
                return row.GetVolumeProfile(overridePlatform).LoadAsync().Result;
            }

            return null;
        }
        
        public float GetPriority(DynamicVolumeType volumeType)
        {
            if (_profileRows.TryGetValue(volumeType, out var row))
            {
                return row.priority;
            }

            return 0;
        }
    }
}