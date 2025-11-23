using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.Util;
#else
using System.Text;
#endif

namespace Chris.Gameplay.Mod
{
    public static class ModAPI
    {
        internal const string DefaultAPIVersion = "0.1.0";

        internal const string DynamicLoadPath = "{LOCAL_MOD_PATH}";

        /// <summary>
        /// Default mod loading directory path.
        /// </summary>
#if !UNITY_EDITOR && UNITY_ANDROID
        public static readonly string LoadingPath = Path.Combine(Application.persistentDataPath, "Mods");
#else
        public static readonly string LoadingPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Mods");
#endif
        
        private static readonly ReactiveProperty<bool> InitializedProperty = new(false);

        private static readonly Subject<Unit> RefreshSubject = new();

        private static readonly List<ModInfo> ModInfos = new();

        private static ModConfig _config;

        /// <summary>
        /// Event when mod api is initialized.
        /// </summary>
        public static ReadOnlyReactiveProperty<bool> Initialized => InitializedProperty;

        /// <summary>
        /// Event when any mod info state is changed.
        /// </summary>
        public static Observable<Unit> Refresh => RefreshSubject;

        /// <summary>
        /// Initialize api and load all mods
        /// </summary>
        /// <param name="modConfig"></param>
        /// <param name="modLoader"></param>
        /// <returns></returns>
        public static async UniTask Initialize(ModConfig modConfig, IModLoader modLoader = default)
        {
            if (InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is already initialized");
                return;
            }
            Debug.Log("[Mod API] Initialize mod api...");
            modLoader ??= new ModLoader(modConfig, new APIValidator(modConfig.ApiVersion));
            _config = modConfig;
            if (await modLoader.LoadAllModsAsync(ModInfos))
            {
                for (int i = _config.States.Count - 1; i >= 0; i--)
                {
                    var state = _config.States[i];
                    if (ModInfos.All(y => y.FullName != state.fullName))
                    {
                        Debug.LogWarning($"[Mod API] Missing mod {state.fullName}");
                        _config.States.RemoveAt(i);
                    }
                }
                InitializedProperty.Value = true;
            }
        }

        /// <summary>
        /// Delete mod on next launch
        /// </summary>
        /// <param name="modInfo"></param>
        public static void DeleteMod(ModInfo modInfo)
        {
            if (!InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return;
            }
            if (_config.GetModState(modInfo) == ModStatus.Delete) return;
            _config.DeleteMod(modInfo);
            ModInfos.Remove(modInfo);
            RefreshSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// Enable mod on next launch
        /// </summary>
        /// <param name="modInfo"></param>
        /// <param name="isEnabled"></param>
        public static void EnabledMod(ModInfo modInfo, bool isEnabled)
        {
            if (!InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return;
            }
            if (_config.GetModState(modInfo) == (isEnabled ? ModStatus.Enabled : ModStatus.Disabled)) return;
            _config.SetModEnabled(modInfo, isEnabled);
            RefreshSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// Get mod state
        /// </summary>
        /// <param name="modInfo"></param>
        /// <returns></returns>
        public static ModStatus GetModState(ModInfo modInfo)
        {
            if (!InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return ModStatus.Disabled;
            }
            return _config.GetModState(modInfo);
        }

        /// <summary>
        /// Get all enabled mod infos
        /// </summary>
        /// <returns></returns>
        public static List<ModInfo> GetAllInfos()
        {
            if (!InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return new List<ModInfo>();
            }
            return ModInfos.ToList();
        }

        /// <summary>
        /// Unzip all zip files from <see cref="path"/> 
        /// </summary>
        /// <param name="path">The root directory path</param>
        /// <param name="allDirectories">Whether to search all directories</param>
        public static void UnZipAll(string path, bool allDirectories)
        {
            var zips = Directory.GetFiles(path, "*.zip", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
            foreach (var zip in zips)
            {
                ZipWrapper.UnzipFile(zip, Path.GetDirectoryName(zip));
                File.Delete(zip);
            }
        }

        /// <summary>
        /// Delete mod files from disk, is not safe when is already initialized,
        /// recommend to use <see cref="DeleteMod"/> to delete mod on next launcher
        /// </summary>
        /// <param name="modInfo"></param>
        public static void DeleteModFromDisk(ModInfo modInfo)
        {
            Directory.Delete(modInfo.FilePath, true);
        }

        private static string GetCatalogExtension()
        {
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
            return ".bin";
#else
            return ".json";
#endif
        }
        
        /// <summary>
        /// Load mod Addressables content catalog from path
        /// </summary>
        /// <param name="path">Can be mod folder path or catalog path</param>
        /// <returns></returns>
        public static async UniTask<bool> LoadModCatalogAsync(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }
            
            string catalogPath = Path.Combine(path, $"catalog{GetCatalogExtension()}");
            if (File.Exists(catalogPath))
            {
                path = catalogPath;
            }
            else
            {
                Debug.LogError($"[Mod API] No catalog file found in {path}");
                return false;
            }

            path = path.Replace(@"\", "/");
            string actualPath = Path.GetDirectoryName(path)!.Replace(@"\", "/");

            try
            {
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
                await ProcessBinaryCatalog(path, actualPath);
#else
                await ProcessJsonCatalog(path, actualPath);
#endif
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Mod API] Unexpected error during process catalog {path}: {e.Message}");
                return false;
            }
        }

#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
        private static async Task ProcessBinaryCatalog(string path, string actualPath)
        {
            // Load the binary catalog
            var data = await File.ReadAllBytesAsync(path);
            var reader = new BinaryStorageBuffer.Reader(data, 1024, 1024, new ContentCatalogData.Serializer().WithInternalIdResolvingDisabled());
            var catalogData = reader.ReadObject<ContentCatalogData>(0, out _, false);

            // Create locator to access catalog data
            var locator = catalogData.CreateCustomLocator();

            // Build a map of primary key to location and keys
            var pkToLoc = new Dictionary<string, (UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation, HashSet<object>)>();
            foreach (var key in locator.Keys)
            {
                if (locator.Locate(key, typeof(object), out var locs))
                {
                    foreach (var loc in locs)
                    {
                        if (!pkToLoc.TryGetValue(loc.PrimaryKey, out var locKeys))
                            pkToLoc.Add(loc.PrimaryKey, locKeys = (loc, new HashSet<object>()));
                        locKeys.Item2.Add(key);
                    }
                }
            }

            // Create new modified entries
            var modifiedEntries = new List<ContentCatalogDataEntry>();
            foreach (var kvp in pkToLoc)
            {
                var loc = kvp.Value.Item1;
                string modifiedInternalId = loc.InternalId.Replace(DynamicLoadPath, actualPath);

                // Collect dependencies
                List<object> deps = null;
                if (loc.HasDependencies)
                {
                    deps = new List<object>();
                    foreach (var d in loc.Dependencies)
                        deps.Add(d.PrimaryKey);
                }

                // Create new entry with modified InternalId
                var newEntry = new ContentCatalogDataEntry(
                    loc.ResourceType,
                    modifiedInternalId,
                    loc.ProviderId,
                    kvp.Value.Item2,
                    deps,
                    loc.Data
                );
                modifiedEntries.Add(newEntry);
            }

            // Create new catalog with modified data
            var newCatalog = new ContentCatalogData(catalogData.ProviderId)
            {
                BuildResultHash = catalogData.BuildResultHash,
                InstanceProviderData = catalogData.InstanceProviderData,
                SceneProviderData = catalogData.SceneProviderData,
                ResourceProviderData = catalogData.ResourceProviderData
            };
            new ContentCatalogDataWrapper(newCatalog).SetData(modifiedEntries);

            // Serialize and save
            var wr = new BinaryStorageBuffer.Writer(0, new ContentCatalogData.Serializer());
            wr.WriteObject(newCatalog, false);
            await File.WriteAllBytesAsync(path, wr.SerializeToByteArray());
            Debug.Log($"[Mod API] Load mod binary content catalog {path}");
            await Addressables.LoadContentCatalogAsync(path).ToUniTask();
            await File.WriteAllBytesAsync(path, data);
        }
#else
        private static async Task ProcessJsonCatalog(string path, string actualPath)
        {
            string contentCatalog = await File.ReadAllTextAsync(path, Encoding.UTF8);
            string modifiedCatalog = contentCatalog.Replace(DynamicLoadPath, actualPath);
            await File.WriteAllTextAsync(path, modifiedCatalog, Encoding.UTF8);
            Debug.Log($"[Mod API] Load mod json content catalog {path}");
            await Addressables.LoadContentCatalogAsync(path).ToUniTask();
            await File.WriteAllTextAsync(path, contentCatalog, Encoding.UTF8);
        }
#endif

        /// <summary>
        /// Load <see cref="ModInfo"/> from path
        /// </summary>
        /// <param name="modInfoPath"></param>
        /// <returns></returns>
        public static async UniTask<ModInfo> LoadModInfo(string modInfoPath)
        {
            var modInfo = JsonConvert.DeserializeObject<ModInfo>(await File.ReadAllTextAsync(modInfoPath));
            modInfo.FilePath = Path.GetDirectoryName(modInfoPath)!.Replace(@"\", "/");
            return modInfo;
        }
        
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
        private readonly struct ContentCatalogDataWrapper
        {
            private static readonly FieldInfo EntriesFieldInfo;

            private readonly ContentCatalogData _catalog;
            
            static ContentCatalogDataWrapper()
            {
                EntriesFieldInfo = typeof(ContentCatalogData).GetField("m_Entries", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public ContentCatalogDataWrapper(ContentCatalogData catalogData)
            {
                _catalog = catalogData;
            }

            public void SetData(IList<ContentCatalogDataEntry> entries)
            {
                EntriesFieldInfo.SetValue(_catalog, entries);
            }
        }
#endif
    }
}