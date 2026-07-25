using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;
using ClientGUI;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Handles map files dropped onto the client window. Dropped map files (and map
    /// files contained in dropped .zip archives) are copied into the custom maps
    /// directory, renamed to the configured map file extension. The existing
    /// <see cref="MapFileWatcher"/> then picks them up and registers them at runtime.
    ///
    /// A map may consist of more than one file: a primary map file plus one or more
    /// supplemental files (see <see cref="ClientConfiguration.SupplementalMapFileExtensions"/>).
    /// These are associated purely by sharing a base file name (e.g. "mymap.map" +
    /// "mymap.bin"), so dropped files are grouped by base name and every file in a
    /// group is imported under a single shared, unique base name to keep them linked.
    /// </summary>
    public class DroppedMapHandler
    {
        /// <summary>
        /// File extensions that are treated as map files. These are renamed to the
        /// configured <see cref="ClientConfiguration.MapFileExtension"/> on import
        /// (e.g. YR's ".yrm" or RA2's ".mpr" become ".map"). The configured extension
        /// is always included.
        /// </summary>
        private static readonly string[] KnownMapFileExtensions = { "map", "yrm", "mpr"};

        private readonly WindowManager windowManager;
        private readonly MapLoader mapLoader;

        public DroppedMapHandler(WindowManager windowManager, MapLoader mapLoader)
        {
            this.windowManager = windowManager;
            this.mapLoader = mapLoader;
        }

        public void Initialize()
        {
#if WINFORMS
            windowManager.FilesDropped += WindowManager_FilesDropped;
#endif
        }

#if WINFORMS
        private void WindowManager_FilesDropped(object sender, Rampastring.XNAUI.PlatformSpecific.FileDropEventArgs e)
        {
            // This event is raised on the WinForms UI thread. Do the file I/O here,
            // then marshal any UI feedback onto the game thread.
            var result = new ImportResult();

            try
            {
                // Content of maps imported during this drop, so that dropping two
                // identical files at once does not import both. Maps already known to
                // the client are deduped separately via MapLoader.FindMapByHash, which
                // reads the SHA1 hashes the client already holds in memory.
                var importedThisDrop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Loose files dropped (or found inside dropped folders) are collected
                // and grouped together so a map and its separately-dropped supplemental
                // files stay associated. Zips are self-contained and handled inline.
                var looseFiles = new List<DroppedFile>();

                foreach (string path in e.FilePaths)
                    CollectDroppedPath(path, looseFiles, importedThisDrop, result);

                ImportGroups(looseFiles, importedThisDrop, result);
            }
            catch (Exception ex)
            {
                Logger.Log($"DroppedMapHandler: Error importing dropped files: {ex.Message}");
            }

            ShowResult(result);
        }
#endif

        /// <summary>
        /// Walks a dropped path. Directories are recursed into, .zip archives are
        /// imported immediately (each archive is its own grouping scope), and loose
        /// map/supplemental files are added to <paramref name="looseFiles"/> to be
        /// grouped and imported together with the rest of the drop.
        /// </summary>
        private void CollectDroppedPath(string path, List<DroppedFile> looseFiles,
            HashSet<string> importedThisDrop, ImportResult result)
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.EnumerateFiles(path))
                    CollectDroppedPath(file, looseFiles, importedThisDrop, result);
                return;
            }

            if (!File.Exists(path))
                return;

            string extension = Path.GetExtension(path).TrimStart('.');

            if (extension.Equals("zip", StringComparison.OrdinalIgnoreCase))
            {
                ImportZip(path, importedThisDrop, result);
                return;
            }

            if (IsMapFileExtension(extension) || IsSupplementalFileExtension(extension))
            {
                string source = path;
                looseFiles.Add(new DroppedFile(
                    Path.GetFileNameWithoutExtension(path),
                    extension,
                    IsMapFileExtension(extension),
                    destination => File.Copy(source, destination, overwrite: false)));
            }
        }

        private void ImportZip(string archivePath, HashSet<string> importedThisDrop, ImportResult result)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(archivePath);

                var files = new List<DroppedFile>();

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip directory entries.
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    string extension = Path.GetExtension(entry.Name).TrimStart('.');

                    if (IsMapFileExtension(extension) || IsSupplementalFileExtension(extension))
                    {
                        ZipArchiveEntry capturedEntry = entry;
                        files.Add(new DroppedFile(
                            Path.GetFileNameWithoutExtension(entry.Name),
                            extension,
                            IsMapFileExtension(extension),
                            destination => capturedEntry.ExtractToFile(destination, overwrite: false)));
                    }
                }

                ImportGroups(files, importedThisDrop, result);
            }
            catch (Exception ex)
            {
                Logger.Log($"DroppedMapHandler: Failed to import archive {archivePath}: {ex.Message}");
                result.Failed++;
            }
        }

        /// <summary>
        /// Groups files by base name and imports each group that contains a primary
        /// map file. Groups without a map file (orphan supplemental files) are skipped.
        /// </summary>
        private void ImportGroups(List<DroppedFile> files, HashSet<string> importedThisDrop, ImportResult result)
        {
            foreach (IGrouping<string, DroppedFile> group in files.GroupBy(f => f.BaseName, StringComparer.OrdinalIgnoreCase))
            {
                DroppedFile map = group.FirstOrDefault(f => f.IsPrimaryMapFile);

                // Without a primary map file the supplemental files have nothing to
                // attach to, so there is nothing meaningful to import.
                if (map == null)
                    continue;

                ImportGroup(map, group.Where(f => !f.IsPrimaryMapFile), importedThisDrop, result);
            }
        }

        /// <summary>
        /// Imports a single logical map (a primary map file plus its supplemental
        /// files) under one shared, unique base name.
        /// </summary>
        private void ImportGroup(DroppedFile map, IEnumerable<DroppedFile> supplementals,
            HashSet<string> importedThisDrop, ImportResult result)
        {
            string customMapsDirectory = EnsureCustomMapsDirectory();

            // Extract the map to a temporary file with a non-map extension first, so
            // the MapFileWatcher does not react to it before we know it is a new map
            // and before its supplemental files are in place.
            string temporaryPath = SafePath.CombineFilePath(customMapsDirectory, $"{Guid.NewGuid():N}.tmp");

            try
            {
                map.ExtractTo(temporaryPath);

                // Skip if the client already knows a map with this content
                // or if we imported it earlier in this drop.
                string hash = Utilities.CalculateSHA1ForFile(temporaryPath);
                if (mapLoader.FindMapByHash(hash) != null || importedThisDrop.Contains(hash))
                {
                    result.Skipped++;
                    return;
                }

                string baseName = GetUniqueBaseName(customMapsDirectory, map.BaseName);

                // Write supplemental files before the map file. The watcher triggers on
                // the map file, and map loading then looks up supplemental files by base
                // name, so they must already be on disk by the time the map appears.
                foreach (DroppedFile supplemental in supplementals)
                {
                    string supplementalDestination =
                        SafePath.CombineFilePath(customMapsDirectory, $"{baseName}.{supplemental.Extension}");
                    supplemental.ExtractTo(supplementalDestination);
                }

                string mapDestination =
                    SafePath.CombineFilePath(customMapsDirectory, $"{baseName}.{ClientConfiguration.Instance.MapFileExtension}");
                File.Move(temporaryPath, mapDestination);
                temporaryPath = null;

                importedThisDrop.Add(hash);
                result.Added++;
            }
            catch (Exception ex)
            {
                Logger.Log($"DroppedMapHandler: Failed to import map {map.BaseName}: {ex.Message}");
                result.Failed++;
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (Exception ex) { Logger.Log($"DroppedMapHandler: Failed to clean up temp file {temporaryPath}: {ex.Message}"); }
                }
            }
        }

        private static bool IsMapFileExtension(string extension)
            => extension.Equals(ClientConfiguration.Instance.MapFileExtension, StringComparison.OrdinalIgnoreCase)
               || KnownMapFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

        private static bool IsSupplementalFileExtension(string extension)
            => ClientConfiguration.Instance.SupplementalMapFileExtensions
                .Contains(extension, StringComparer.OrdinalIgnoreCase);

        private static string EnsureCustomMapsDirectory()
        {
            string customMapsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, MapLoader.CUSTOM_MAPS_DIRECTORY);
            DirectoryInfo directoryInfo = SafePath.GetDirectory(customMapsDirectory);
            if (!directoryInfo.Exists)
                directoryInfo.Create();

            return customMapsDirectory;
        }

        /// <summary>
        /// Finds a base file name (without extension) for which no map file yet exists
        /// in the custom maps directory, appending a numeric suffix if necessary. The
        /// whole logical map (map file and supplemental files) shares this base name.
        /// </summary>
        private static string GetUniqueBaseName(string customMapsDirectory, string baseName)
        {
            string mapFileExtension = ClientConfiguration.Instance.MapFileExtension;

            string candidate = baseName;
            int suffix = 1;
            while (File.Exists(SafePath.CombineFilePath(customMapsDirectory, $"{candidate}.{mapFileExtension}")))
            {
                candidate = $"{baseName}_{suffix}";
                suffix++;
            }

            return candidate;
        }

        private void ShowResult(ImportResult result)
        {
            // Nothing relevant was dropped (e.g. unrelated files); stay silent.
            if (result.Added == 0 && result.Skipped == 0 && result.Failed == 0)
                return;

            var lines = new List<string>();

            if (result.Added > 0)
                lines.Add(string.Format("{0} map(s) imported.".L10N("Client:Main:MapsImported"), result.Added));
            if (result.Skipped > 0)
                lines.Add(string.Format("{0} map(s) already present.".L10N("Client:Main:MapsAlreadyPresent"), result.Skipped));
            if (result.Failed > 0)
                lines.Add(string.Format("{0} file(s) failed to import.".L10N("Client:Main:MapsImportFailed"), result.Failed));

            if (result.Added > 0)
                lines.Add("Imported maps are now available in the map list.".L10N("Client:Main:MapsImportedAvailable"));

            string message = string.Join(Environment.NewLine, lines);

            windowManager.AddCallback(new Action(() =>
                XNAMessageBox.Show(windowManager, "Map Import".L10N("Client:Main:MapImportTitle"), message)));
        }

        /// <summary>
        /// A single dropped file (loose or a zip entry) that is a candidate for import,
        /// together with how to write its contents to a destination path.
        /// </summary>
        private sealed class DroppedFile
        {
            public DroppedFile(string baseName, string extension, bool isMap, Action<string> extractTo)
            {
                BaseName = baseName;
                Extension = extension;
                IsPrimaryMapFile = isMap;
                this.extractTo = extractTo;
            }

            public string BaseName { get; }

            /// <summary>The original file extension, without a leading dot.</summary>
            public string Extension { get; }

            public bool IsPrimaryMapFile { get; }

            private readonly Action<string> extractTo;

            public void ExtractTo(string destination) => extractTo(destination);
        }

        private sealed class ImportResult
        {
            public int Added;
            public int Skipped;
            public int Failed;
        }
    }
}
