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

