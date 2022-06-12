namespace ClientUpdater
{
    public class UpdaterFileInfo
    {
        public string Filename { get; set; }

        public string Identifier { get; set; }

        public int Size { get; set; }

        public string ArchiveIdentifier { get; set; }

        public int ArchiveSize { get; set; }

        public bool Archived
        {
            get
            {
                if (!string.IsNullOrEmpty(ArchiveIdentifier))
                {
                    return ArchiveSize > 0;
                }
                return false;
            }
        }

        public UpdaterFileInfo()
        {
        }

        public UpdaterFileInfo(string filename, string identifier, int size, string archiveIdentifier = null, int archiveSize = 0)
        {
            Filename = filename;
            Identifier = identifier;
            Size = size;
            ArchiveIdentifier = archiveIdentifier;
            ArchiveSize = archiveSize;
        }
    }
}