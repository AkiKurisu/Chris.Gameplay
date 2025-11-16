using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Chris.Gameplay.Mod
{
    public static class ModAPI
    {
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
                _config.stateInfos.RemoveAll(x => ModInfos.All(y => y.FullName != x.modFullName));
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
            if (_config.GetModState(modInfo) == ModState.Delete) return;
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
            if (_config.GetModState(modInfo) == (isEnabled ? ModState.Enabled : ModState.Disabled)) return;
            _config.SetModEnabled(modInfo, isEnabled);
            RefreshSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// Get mod state
        /// </summary>
        /// <param name="modInfo"></param>
        /// <returns></returns>
        public static ModState GetModState(ModInfo modInfo)
        {
            if (!InitializedProperty.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return ModState.Disabled;
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
                return new();
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

        /// <summary>
        /// Load mod Addressables content catalog from path
        /// </summary>
        /// <param name="path">Can be mod folder path or catalog path</param>
        /// <returns></returns>
        public static async UniTask<bool> LoadModCatalogAsync(string path)
        {
            if (Directory.Exists(path))
            {
                // Check for both JSON and Binary catalog formats
                string jsonPath = Path.Combine(path, "catalog.json");
                string binPath = Path.Combine(path, "catalog.bin");

                if (File.Exists(jsonPath))
                {
                    path = jsonPath;
                }
                else if (File.Exists(binPath))
                {
                    path = binPath;
                }
                else
                {
                    Debug.LogError($"[Mod API] No catalog file found in {path}");
                    return false;
                }
            }

            if (!File.Exists(path))
            {
                return false;
            }

            path = path.Replace(@"\", "/");
            string actualPath = Path.GetDirectoryName(path)!.Replace(@"\", "/");
            string extension = Path.GetExtension(path).ToLower();

            if (extension == ".json")
            {
                // JSON Catalog processing
                string contentCatalog = await File.ReadAllTextAsync(path, Encoding.UTF8);
                string modifiedCatalog = contentCatalog.Replace(ImportConstants.DynamicLoadPath, actualPath);
                await File.WriteAllTextAsync(path, modifiedCatalog, Encoding.UTF8);
                Debug.Log($"[Mod API] Load mod JSON content catalog {path}");
                await Addressables.LoadContentCatalogAsync(path).ToUniTask();
                await File.WriteAllTextAsync(path, contentCatalog, Encoding.UTF8);
            }
            else if (extension == ".bin")
            {
                // Binary Catalog processing: Load, modify entries, re-serialize
                string tempPath = path + ".tmp";

                try
                {
                    var success = ModifyCatalogPaths(path, tempPath, actualPath);

                    if (success)
                    {
                        Debug.Log($"[Mod API] Load mod Binary content catalog {tempPath}");
                        await Addressables.LoadContentCatalogAsync(tempPath).ToUniTask();
                        File.Delete(tempPath);
                    }
                    else
                    {
                        Debug.LogError($"[Mod API] Failed to modify binary catalog paths");
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Mod API] Error processing binary catalog: {ex.Message}");
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    return false;
                }
            }
            else
            {
                Debug.LogError($"[Mod API] Unsupported catalog format: {extension}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Modify catalog internal paths and save to a new file
        /// </summary>
        /// <param name="sourcePath">Source catalog path</param>
        /// <param name="targetPath">Target catalog path</param>
        /// <param name="newBasePath">New base path to replace DynamicLoadPath</param>
        /// <returns>True if successful</returns>
        private static bool ModifyCatalogPaths(string sourcePath, string targetPath, string newBasePath)
        {
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
            // Load the binary catalog
            var catalogData = ContentCatalogData.LoadFromFile(sourcePath, false);

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
                string modifiedInternalId = loc.InternalId.Replace(ImportConstants.DynamicLoadPath, newBasePath);

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
            var newCatalog = new ContentCatalogData(modifiedEntries, catalogData.ProviderId)
            {
                BuildResultHash = catalogData.BuildResultHash,
                InstanceProviderData = catalogData.InstanceProviderData,
                SceneProviderData = catalogData.SceneProviderData,
                ResourceProviderData = catalogData.ResourceProviderData
            };
            newCatalog.SetData(modifiedEntries);

            // Serialize and save
            var bytes = newCatalog.SerializeToByteArray();
            File.WriteAllBytes(targetPath, bytes);

            return true;
#else
            // For JSON catalogs, this method shouldn't be called
            Debug.LogError("[Mod API] ModifyCatalogPaths should not be called for JSON catalogs");
            return false;
#endif
        }

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

    }
}