using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Chris.Mod
{
    public static class ModAPI
    {
        public static ReactiveProperty<bool> IsModInit { get; } = new(false);

        public static Subject<Unit> OnModRefresh { get; } = new();

        private static readonly List<ModInfo> ModInfos = new();

        private static ModConfig _config;

        /// <summary>
        /// Initialize api and load all mods
        /// </summary>
        /// <param name="modConfig"></param>
        /// <param name="modLoader"></param>
        /// <returns></returns>
        public static async UniTask Initialize(ModConfig modConfig, IModLoader modLoader = default)
        {
            if (IsModInit.Value)
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
                IsModInit.Value = true;
            }
        }

        /// <summary>
        /// Delete mod on next launch
        /// </summary>
        /// <param name="modInfo"></param>
        public static void DeleteMod(ModInfo modInfo)
        {
            if (!IsModInit.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return;
            }
            if (_config.GetModState(modInfo) == ModState.Delete) return;
            _config.DeleteMod(modInfo);
            ModInfos.Remove(modInfo);
            OnModRefresh.OnNext(Unit.Default);
        }

        /// <summary>
        /// Enable mod on next launch
        /// </summary>
        /// <param name="modInfo"></param>
        /// <param name="isEnabled"></param>
        public static void EnabledMod(ModInfo modInfo, bool isEnabled)
        {
            if (!IsModInit.Value)
            {
                Debug.LogError("[Mod API] Mod api is not initialized");
                return;
            }
            if (_config.GetModState(modInfo) == (isEnabled ? ModState.Enabled : ModState.Disabled)) return;
            _config.SetModEnabled(modInfo, isEnabled);
            OnModRefresh.OnNext(Unit.Default);
        }

        /// <summary>
        /// Get mod state
        /// </summary>
        /// <param name="modInfo"></param>
        /// <returns></returns>
        public static ModState GetModState(ModInfo modInfo)
        {
            if (!IsModInit.Value)
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
            if (!IsModInit.Value)
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
                path = Path.Combine(path, "catalog.json");
            }
            if (!File.Exists(path))
            {
                return false;
            }
            path = path.Replace(@"\", "/");
            string contentCatalog = File.ReadAllText(path, Encoding.UTF8);
            File.WriteAllText(path, contentCatalog.Replace(ImportConstants.DynamicLoadPath, Path.GetDirectoryName(path)!.Replace(@"\", "/")), Encoding.UTF8);
            Debug.Log($"[Mod API] Load mod content catalog {path}");
            await Addressables.LoadContentCatalogAsync(path).ToUniTask();
            File.WriteAllText(path, contentCatalog, Encoding.UTF8);
            return true;
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