namespace Updater
{
    public class UpdateMirror
    {
        public string Url { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public UpdateMirror()
        {
        }

        public UpdateMirror(string url, string name, string location)
        {
            Url = url;
            Name = name;
            Location = location;
        }
    }
}