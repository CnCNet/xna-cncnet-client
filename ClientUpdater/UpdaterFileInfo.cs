namespace ClientUpdater
{
    /// <summary>
    ///  Updater file info.
    /// </summary>
    public class UpdaterFileInfo
    {
        /// <summary>
        /// Create new updater file info instance.
        /// </summary>
        public UpdaterFileInfo()
        {
        }

        /// <summary>
        /// Create new updater file info instance from given information.
        /// </summary>
        public UpdaterFileInfo(string filename, string identifier, int size, string archiveIdentifier = null, int archiveSize = 0)
        {
            Filename = filename;
            Identifier = identifier;
            Size = size;
            ArchiveIdentifier = archiveIdentifier;
            ArchiveSize = archiveSize;
        }

        /// <summary>
        /// Filename of the file.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// File identifier for the file.
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Size of the file.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// File identifier of the compressed archive for the file.
        /// </summary>
        public string ArchiveIdentifier { get; set; }

        /// <summary>
        /// Size of compressed archive for the file.
        /// </summary>
        public int ArchiveSize { get; set; }

        /// <summary>
        /// Whether or not the file is compressed archive.
        /// </summary>
        public bool Archived => !string.IsNullOrEmpty(ArchiveIdentifier) && ArchiveSize > 0;
    }
}

