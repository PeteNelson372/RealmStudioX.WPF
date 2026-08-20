using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Models.Startup
{
    public class CreateOpenPackageResult
    {
        public RealmCreationOperation CreationOperation { get; set; } = RealmCreationOperation.NotSet;
        public RealmProjectType ProjectType { get; set; } = RealmProjectType.NotSet;
        public RealmMapType MapType { get; set; } = RealmMapType.NotSet;
        public bool IsNew { get; set; }
        public RealmStudioProject? Project { get; set; }
        public string? MapName { get; set; }
        public string? FilePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string MapAreaUnits { get; set; } = string.Empty;
        public float MapAreaWidth { get; set; }
        public float MapAreaHeight { get; set; }
        public string? Theme { get; set; }
    }
}
