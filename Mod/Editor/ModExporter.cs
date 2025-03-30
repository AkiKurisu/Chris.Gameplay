using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using Newtonsoft.Json;

namespace Chris.Mod.Editor
{
    public class ModExporter
    {
        private readonly List<IModBuilder> _builders;

        private readonly ModExportConfig _exportConfig;
        
        public ModExporter(ModExportConfig exportConfig)
        {
            _exportConfig = exportConfig;
            _builders = new List<IModBuilder>
            {
                new PathBuilder(),
            };
            _builders.AddRange(exportConfig.customBuilders.Select(serializedType => serializedType.GetObject()));
        }
        
        private static string CreateBuildPath(string modName)
        {
            var targetPath = Path.Combine(ExportConstants.ExportPath, EditorUserBuildSettings.activeBuildTarget.ToString());
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            var buildPath = Path.Combine(targetPath, modName.Replace(" ", string.Empty));
            if (Directory.Exists(buildPath)) FileUtil.DeleteFileOrDirectory(buildPath);
            Directory.CreateDirectory(buildPath);
            return buildPath;
        }
        
        public bool Export()
        {
            string buildPath = _exportConfig.lastExportPath = CreateBuildPath(_exportConfig.modName);
            BuildPipeline(buildPath);
            WritePipeline(buildPath);
            if (BuildContent())
            {
                string achievePath = buildPath + ".zip";
                if (!ZipTogether(buildPath, achievePath))
                {
                    LogError($"Zip failed!");
                    return false;
                }
                Directory.Delete(buildPath, true);
                Log($"Export succeed, export path: {achievePath}");
                return true;
            }

            LogError($"Build pipeline failed!");
            return false;
        }
        
        private static void LogError(string message)
        {
            Debug.LogError($"<color=#ff2f2f>Exporter</color>: {message}");
        }
        
        private static void Log(string message)
        {
            Debug.Log($"<color=#3aff48>Exporter</color>: {message}");
        }
        
        private static bool ZipTogether(string buildPath, string zipPath)
        {
            return ZipWrapper.Zip(new[] { buildPath }, zipPath);
        }
        
        private void WritePipeline(string buildPath)
        {
            var info = new ModInfo
            {
                authorName = _exportConfig.authorName,
                description = _exportConfig.description,
                modName = _exportConfig.modName,
                version = _exportConfig.version,
                modIconBytes = _exportConfig.modIcon != null ? _exportConfig.modIcon.EncodeToPNG() : Array.Empty<byte>(),
                apiVersion = ImportConstants.APIVersion.ToString(CultureInfo.InvariantCulture)
            };
            foreach (var builder in _builders)
            {
                builder.Write(ref info);
            }
            var stream = JsonConvert.SerializeObject(info);
            File.WriteAllText(buildPath + "/ModConfig.cfg", stream);
        }
        
        private bool BuildContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            CleanupPipeline();
            return string.IsNullOrEmpty(result.Error);
        }
        
        private void BuildPipeline(string dynamicBuildPath)
        {
            foreach (var builder in _builders)
            {
                builder.Build(_exportConfig, dynamicBuildPath);
            }
        }
        
        private void CleanupPipeline()
        {
            foreach (var builder in _builders)
            {
                builder.Cleanup(_exportConfig);
            }
        }
    }
}
