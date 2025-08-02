namespace Chris.Mod.Editor
{
    public abstract class CustomBuilder : IModBuilder
    {
        public abstract string Description { get; }
        
        public virtual void Build(ModExportConfig exportConfig, string buildPath)
        {

        }

        public virtual void Cleanup(ModExportConfig exportConfig)
        {

        }

        public void Write(ModInfo modInfo)
        {

        }
    }
}