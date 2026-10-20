using System.IO;
using System.Media;

namespace RealmStudioX.WPF.Editor.Services;

public static class SoundService
{
    private static SoundPlayer? _player;

    public static void PlaySound(string soundName)
    {
        string soundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sounds", soundName);

        if (!File.Exists(soundPath))
        {
            return;
        }

        _player?.Stop();
        _player = new SoundPlayer(soundPath);
        _player.Play();
    }
}