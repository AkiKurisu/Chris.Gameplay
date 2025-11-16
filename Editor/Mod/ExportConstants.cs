using UnityEngine;
using System.IO;
namespace Chris.Gameplay.Mod.Editor
{
    public class ExportConstants
    {
        private static readonly LazyDirectory ExportDirectory = new(Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Export"));
        
        public static string ExportPath => ExportDirectory.GetPath();
    }
    
    public class LazyDirectory
    {
        private readonly string _path;
        
        private bool _initialized;
        
        public LazyDirectory(string path)
        {
            _path = path;
        }
        
        public string GetPath()
        {
            if (!_initialized) return _path;
            
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            _initialized = true;
            return _path;
        }
    }
}
