namespace Chris.Mod.Editor
{
    public interface IModBuilder
    {
        /// <summary>
        /// Preprocess for mod assets
        /// </summary>
        /// <param name="exportConfig"></param>
        /// <param name="buildPath"></param>
        void Build(ModExportConfig exportConfig, string buildPath);
        
        /// <summary>
        /// Write meta data
        /// </summary>
        /// <param name="modInfo"></param>
        void Write(ModInfo modInfo);
        
        /// <summary>
        /// Clean after build
        /// </summary>
        /// <param name="exportConfig"></param>
        void Cleanup(ModExportConfig exportConfig);
    }
}
