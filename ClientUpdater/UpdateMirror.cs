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
    /// Update mirror info.
    /// </summary>
    public class UpdateMirror
    {
        /// <summary>
        /// Create new update mirror info instance.
        /// </summary>
        public UpdateMirror()
        {
        }

        /// <summary>
        /// Create new update mirror info instance from given information.
        /// </summary>
        public UpdateMirror(string url, string name, string location)
        {
            URL = url;
            Name = name;
            Location = location;
        }

        /// <summary>
        /// Update mirror URL.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Update mirror name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Update mirror location.
        /// </summary>
        public string Location { get; set; }

    }
}

