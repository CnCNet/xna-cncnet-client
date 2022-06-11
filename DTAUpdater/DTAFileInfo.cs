namespace Updater
{
    public class DTAFileInfo
    {
        private string afileName;

        private string aIdentifier;

        private int aSize;

        private int aVersion;

        public string Name
        {
            get
            {
                return afileName;
            }
            set
            {
                afileName = value;
            }
        }

        public string Identifier
        {
            get
            {
                return aIdentifier;
            }
            set
            {
                aIdentifier = value;
            }
        }

        public int Version
        {
            get
            {
                return aVersion;
            }
            set
            {
                aVersion = value;
            }
        }

        public int Size
        {
            get
            {
                return aSize;
            }
            set
            {
                aSize = value;
            }
        }

        public DTAFileInfo()
        {
        }

        public DTAFileInfo(string _name, int _version)
        {
            Name = _name;
            Version = _version;
        }
    }
}