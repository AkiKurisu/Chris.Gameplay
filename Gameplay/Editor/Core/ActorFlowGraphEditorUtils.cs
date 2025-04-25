using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ceres.Graph;
using Ceres.Graph.Flow;
using Chris.Serialization;
using UnityEditor;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace Chris.Gameplay.Editor
{
    public static class ActorFlowGraphEditorUtils
    {
        public static void ExportRemoteAsset(IFlowGraphContainer container, string address)
        {
            var flowPath = Path.Combine(SaveUtility.SavePath, "Flow");
            if (!ActorFlowGraphDataTableManager.Get().TryGetRemoteUpdatePath(address, out var path))
            {
                path = address;
            }
            var data = container.GetFlowGraphData();
            var referencedAssets = data.subGraphData.SelectMany(x => x.graphData.nodeData)
                .Concat(data.nodeData)
                .SelectMany(node => node.GetReferencedObjects()).ToArray();
            var assetPaths = referencedAssets.Where(x => (bool)x)
                .Select(AssetDatabase.GetAssetPath)
                .Where(assetPath => !string.IsNullOrEmpty(assetPath))
                .ToList();
            if (assetPaths.Any())
            {
                BuildRemoteAssetBundle(container, path, data, assetPaths, flowPath);
            }
            else
            {
                BuildRemoteAssetBinary(container, flowPath, path, data);
            }
            Process.Start(flowPath);
        }

        private static void BuildRemoteAssetBinary(IFlowGraphContainer container, string flowPath, string path,
            FlowGraphData data)
        {
            var serializer = new SaveLoadSerializer(flowPath, "bin");
            serializer.Save(path, data);
            UDebug.Log($"Export {container.GetIdentifier()} remote asset to {Path.Combine(flowPath, $"{path}.bin")}");
        }

        private static void BuildRemoteAssetBundle(IFlowGraphContainer container, string path, FlowGraphData data,
            List<string> assetPaths, string flowPath)
        {
            var tempPath = $"Assets/{path}.asset";
            var asset = ScriptableObject.CreateInstance<FlowGraphAsset>();
            AssetDatabase.CreateAsset(asset, tempPath);
            AssetDatabase.Refresh();
            asset.SetGraphData(data.CloneT<FlowGraphData>());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            assetPaths.Insert(0, AssetDatabase.GetAssetPath(asset));
            var addressableNames = assetPaths.ToArray();
            addressableNames[0] = path;
            var buildMap = new AssetBundleBuild
            {
                assetBundleName = $"{path}.bundle",
                assetNames = assetPaths.ToArray(),
                addressableNames = addressableNames
            };
            var manifest = BuildPipeline.BuildAssetBundles(
                flowPath,
                new[] { buildMap },
                BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows);
            var ab =  manifest.GetAllAssetBundles()[0];
            var abName = Path.Combine(flowPath, ab);
            var destName = Path.Combine(flowPath, $"{path}.bundle");
            File.Delete(abName + ".manifest");
            File.Delete(Path.Combine(flowPath, "Flow"));
            File.Delete(Path.Combine(flowPath, "Flow.manifest"));
            UDebug.Log($"Export {container.GetIdentifier()} remote asset to {destName}");
        }
    }
}