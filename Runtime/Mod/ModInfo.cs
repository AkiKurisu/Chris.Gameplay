using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Chris.Gameplay.Mod
{
    /// <summary>
    /// Class defines mod's information
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        #region Serialized Field
        public string apiVersion;
        
        public string authorName;
        
        public string modName;
        
        public string version;
        
        public string description;
        
        public byte[] modIconBytes;
        
        public Dictionary<string, string> metaData = new();
        #endregion
        
        [JsonIgnore]
        public string FilePath { get; set; }
        
        [JsonIgnore]
        public string FullName => modName + '-' + version + '-' + apiVersion;
    }
}