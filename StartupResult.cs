namespace RealmStudioX.WPF
{
    public class StartupResult
    {
        public bool IsNew { get; set; }
        public string? FilePath { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public string? Theme { get; set; }
    }
}
