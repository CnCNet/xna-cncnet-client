namespace ClientUpdater
{
    public class UpdateMirror
    {
        public string URL { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public UpdateMirror()
        {
        }

        public UpdateMirror(string url, string name, string location)
        {
            URL = url;
            Name = name;
            Location = location;
        }
    }
}