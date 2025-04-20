using System.Collections.Generic;
using System.IO;
using Ceres.Graph.Flow;
using Chris.Serialization;
using UnityEngine;

namespace Chris.Gameplay
{
    [InitializeOnWorldCreate]
    public class ActorFlowGraphSubsystem: WorldSubsystem
    {
        private readonly SaveLoadSerializer _serializer = new (Path.Combine(SaveUtility.SavePath, "Flow"), "bin");

        private readonly Dictionary<string, FlowGraphAsset> _remoteFlowGraph = new();
        
        public static ActorFlowGraphSubsystem Get()
        {
            return GameWorld.Get()?.GetSubsystem<ActorFlowGraphSubsystem>();
        }

        public FlowGraphAsset GetFlowGraphAsset(string address)
        {
            // Already loaded
            if (ActorFlowGraphDataTableManager.Get().TryGetFlowGraphAsset(address, out var flowGraphAsset))
            {
                return flowGraphAsset;
            }
            
            // Remote update flow graph
            if (ActorFlowGraphDataTableManager.Get().TryGetRemoteUpdatePath(address, out var remotePath))
            {
                if (_remoteFlowGraph.TryGetValue(remotePath, out var asset))
                {
                    return asset;
                }
                if (_serializer.Exists(remotePath))
                {
                    asset = ScriptableObject.CreateInstance<FlowGraphAsset>();
                    var data = _serializer.Load<FlowGraphData>(remotePath);
                    asset.SetGraphData(data);
                    _remoteFlowGraph.Add(remotePath, asset);
                    return asset;
                }
            }

            return null;
        }

        protected override void Release()
        {
            _remoteFlowGraph.Clear();
            base.Release();
        }
    }
}