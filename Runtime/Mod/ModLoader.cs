using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;

namespace Chris.Gameplay.Mod
{
    public interface IModValidator
    {
        bool ValidateMod(ModInfo modInfo);
    }
    
    public interface IModLoader
    {
        UniTask<bool> LoadAllModsAsync(List<ModInfo> modInfos);
    }
    
    public class APIValidator : IModValidator
    {
        private readonly Version _apiVersion;
        
        public APIValidator(string apiVersion)
        {
            if (!Version.TryParse(apiVersion, out _apiVersion))
            {
                _apiVersion = new Version(0, 1, 0);
            }
        }
        
        public bool ValidateMod(ModInfo modInfo)
        {
            if (Version.TryParse(modInfo.apiVersion, out var modVersion))
            {
                return modVersion.CompareTo(_apiVersion) == 0;
            }
            return false;
        }
    }
    
    public class ModLoader : IModLoader
    {
        private readonly ModConfig _modConfigData;
        
        private readonly IModValidator _validator;
        
        public ModLoader(ModConfig modConfigData, IModValidator validator)
        {
            _modConfigData = modConfigData;
            _validator = validator;
        }
        
        public async UniTask<bool> LoadAllModsAsync(List<ModInfo> modInfos)
        {
            string modPath = _modConfigData.LoadingPath;
            if (!Directory.Exists(modPath))
            {
                Directory.CreateDirectory(modPath);
                return true;
            }
            ModAPI.UnZipAll(modPath, true);
            var directories = Directory.GetDirectories(modPath, "*", SearchOption.AllDirectories);
            if (directories.Length == 0)
            {
                return true;
            }
            List<string> configPaths = new();
            List<string> directoryPaths = new();
            foreach (var directory in directories)
            {
                string[] files = Directory.GetFiles(directory, "*.cfg");
                if (files.Length != 0)
                {
                    configPaths.AddRange(files);
                    directoryPaths.Add(directory);
                }
            }
            if (configPaths.Count == 0)
            {
                return true;
            }
            for (int i = configPaths.Count - 1; i >= 0; i--)
            {
                var modInfo = await ModAPI.LoadModInfo(configPaths[i]);
                var state = _modConfigData.GetModState(modInfo);
                if (state == ModState.Enabled)
                {
                    modInfos.Add(modInfo);
                }
                else if (state == ModState.Disabled)
                {
                    directoryPaths.RemoveAt(i);
                    modInfos.Add(modInfo);
                }
                else
                {
                    ModAPI.DeleteModFromDisk(modInfo);
                    directoryPaths.RemoveAt(i);
                }

            }
            foreach (var directory in directoryPaths)
            {
                await ModAPI.LoadModCatalogAsync(directory);
            }
            return true;
        }
        
        public async UniTask<ModInfo> LoadModAsync(ModConfig configData, string path)
        {
            var configs = Directory.GetFiles(path, "*.cfg");
            if (configs.Length == 0) return null;
            string config = configs[0];
            var modInfo = await ModAPI.LoadModInfo(config);
            var state = configData.GetModState(modInfo);
            if (state == ModState.Enabled)
            {
                if (!_validator.ValidateMod(modInfo))
                {
                    return modInfo;
                }
            }
            else if (state == ModState.Disabled)
            {
                return modInfo;
            }
            else
            {
                ModAPI.DeleteModFromDisk(modInfo);
                return null;
            }
            await ModAPI.LoadModCatalogAsync(path);
            return modInfo;
        }
    }
}