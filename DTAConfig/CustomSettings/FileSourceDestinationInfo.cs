using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.CustomSettings
{
    sealed class FileSourceDestinationInfo
    {
        public FileSourceDestinationInfo(string source, string destination)
        {
            SourcePath = source;
            DestinationPath = destination;
        }

        public string SourcePath { get; private set; }
        public string DestinationPath { get; private set; }
    }
}
