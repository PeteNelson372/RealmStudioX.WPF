using System.Collections.ObjectModel;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialSystem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "New Celestial System";

        public string Description { get; set; } = string.Empty;

        public ObservableCollection<CelestialBodyNode> Bodies { get; } = [];

        public bool Animate { get; set; } = true;
    }
}
