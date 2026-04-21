namespace RealmStudioX.WPF
{
    public class StartupResult
    {
        public bool IsNew { get; set; }
        public string? MapName { get; set; }
        public string? FilePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? MapAreaUnits { get; set; }
        public float? MapAreaWidth { get; set; }
        public float? MapAreaHeight { get; set; }
        public string? Theme { get; set; }
    }
}
