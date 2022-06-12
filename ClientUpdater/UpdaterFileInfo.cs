/*
Copyright 2022 CnCNet

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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

