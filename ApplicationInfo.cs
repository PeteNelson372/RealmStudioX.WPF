using System.Reflection;

namespace RealmStudioX.WPF
{
    public static class ApplicationInfo
    {
        public static string Version
        {
            get
            {
                return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            }
        }
    }
}
