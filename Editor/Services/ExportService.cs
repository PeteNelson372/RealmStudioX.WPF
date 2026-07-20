using Microsoft.Win32;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace RealmStudioX.WPF.Editor.Services
{
    public class ExportService
    {
        private readonly EditorController _editor;

        public ExportService(EditorController editor)
        {
            _editor = editor;
        }

        public void ExportRealm(IRealmExportSettings settings)
        {
            SKEncodedImageFormat exportFileFormat = ToSkiaFormat(settings.RealmExportFormat);

            switch (settings.RealmExportType)
            {
                case RealmExportType.BitmapImage:
                    {
                        ExportMapAsImage(exportFileFormat, false);
                    }
                    break;
                case RealmExportType.UpscaledImage:
                    {
                        ExportMapAsImage(exportFileFormat, true);
                    }
                    break;
                case RealmExportType.MapLayers:
                    {
                        ExportMapAsLayers(exportFileFormat);
                    }
                    break;
                case RealmExportType.RealmStudioMapXml:
                    {
                        ExportMapAsXml();
                    }
                    break;
                case RealmExportType.Heightmap:
                    {
                        // TODO:
                    }
                    break;
                case RealmExportType.HeightMap3DModel:
                    {
                        // TODO:
                    }
                    break;
            }
        }

        private void ExportMapAsXml()
        {
            string filename = GetExportFileName("rsmx", UserInterfaceUtilities.GetRealmStudioMapXmlFileFilter());

            if (!string.IsNullOrEmpty(filename))
            {
                FileStream fs = new(filename, FileMode.Create, FileAccess.Write);

                try
                {
                    MapFileMethods.SaveMap(_editor.Scene!.Map, fs);

                    MessageDialog dlg = MessageDialogFactory.InformationDialog("Map Exported", $"Map exported to {filename}");
                    dlg.ShowDialog();
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("Exception occured while exporting map as RealmStudioMap XML", ex);

                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error exporting map", $"An error occurred while exporting map to {filename}");
                    dlg.ShowDialog();
                }
                finally
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        public static SKEncodedImageFormat ToSkiaFormat(RealmMapExportFormat format)
        {
            return format switch
            {
                RealmMapExportFormat.PNG => SKEncodedImageFormat.Png,
                RealmMapExportFormat.JPG => SKEncodedImageFormat.Jpeg,
                RealmMapExportFormat.BMP => SKEncodedImageFormat.Bmp,
                RealmMapExportFormat.GIF => SKEncodedImageFormat.Gif,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        private void ExportMapAsImage(SKEncodedImageFormat exportFormat, bool upscale = false)
        {
            string filename = GetExportFileName(exportFormat.ToString().ToLowerInvariant(), UserInterfaceUtilities.GetCommonImageFilter());

            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    SKSurface s = SKSurface.Create(new SKImageInfo(_editor.Scene!.Map.MapWidth, _editor.Scene!.Map.MapHeight));
                    s.Canvas.Clear();

                    _editor.Scene!.RenderForExport(s.Canvas);

                    SKImage image = s.Snapshot();

                    if (upscale)
                    {
                        SKBitmap upscaled = Utilities.ResizeBitmap(SKBitmap.FromImage(image), _editor.Scene!.Map.MapWidth * 2, _editor.Scene!.Map.MapHeight * 2);
                        image.Dispose();

                        image = SKImage.FromBitmap(upscaled);
                    }

                    // Save the image to disk
                    SaveImage(image, filename, exportFormat, 100);

                    MessageDialog dlg = MessageDialogFactory.InformationDialog("Map Exported", $"Map exported to {filename}");
                    dlg.ShowDialog();
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("Exception occured while exporting map as an image", ex);

                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error exporting map", $"An error occurred while exporting map to {filename}");
                    dlg.ShowDialog();
                }
            }
        }

        public static void SaveImage(SKImage image, string filename, SKEncodedImageFormat format, int quality = 100)
        {
            using SKData? data = image.Encode(format, quality) ?? throw new InvalidOperationException("Unable to encode image.");
            using FileStream stream = File.Create(filename);
            data.SaveTo(stream);
        }

        private void ExportMapAsLayers(SKEncodedImageFormat exportFormat)
        {
            string filename = GetExportFileName("zip", UserInterfaceUtilities.GetZipFileFilter());

            // create a zip file and add each of the layer bitmaps to it
            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    using (FileStream fileStream = new(filename, FileMode.OpenOrCreate))
                    {
                        ExportMapLayersAsZipFile(fileStream, _editor.Scene!.Map, exportFormat);
                    }

                    MessageBox.Show("Map layers exported to " + filename, "Map Exported", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("Exception occured while exporting map as layers", ex);

                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error exporting map", $"An error occurred while exporting map layers to {filename}");
                    dlg.ShowDialog();
                }
            }
        }

        private void ExportMapLayersAsZipFile(FileStream fileStream, RealmStudioMap map, SKEncodedImageFormat exportFormat)
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false);

            List<LayerExportEntry> layers = _editor.Scene!.RenderAsLayers();

            foreach (LayerExportEntry layerExportEntry in layers)
            {
                string folder = $"Layers/{layerExportEntry.LayerName}/";
                ZipArchiveEntry layer = archive.CreateEntry(folder);

                ZipArchiveEntry layerBitmap =
                    archive.CreateEntry(folder + layerExportEntry.LayerImageName);

                using Stream stream = layerBitmap.Open();

                using SKData data =
                    layerExportEntry.LayerImage.Encode(SKEncodedImageFormat.Png, 100);

                data.SaveTo(stream);
            }
        }

        private static string GetExportFileName(string defaultFormat, string filter)
        {
            SaveFileDialog ofd = new()
            {
                Title = "Export Map",
                DefaultExt = defaultFormat.ToLowerInvariant(),
                RestoreDirectory = true,
                AddExtension = true,
                CheckPathExists = true,
                ValidateNames = true,
                Filter = filter
            };

            int filterIndex = 0;
            string[] filterStrings = ofd.Filter.Split('|');

            for (int i = 2; i < filterStrings.Length; i++)      // skip all image strings filter
            {
                if (filterStrings[i].Contains(defaultFormat, StringComparison.CurrentCultureIgnoreCase))
                {
                    filterIndex = (i / 2) + 1;
                    break;
                }
            }

            ofd.FilterIndex = filterIndex;

            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                return ofd.FileName;

            }
            else
            {
                return string.Empty;
            }
        }
    }
}
