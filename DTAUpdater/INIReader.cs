using System.IO;

namespace Updater
{
    internal class INIReader
    {
        private StreamReader streamReader;

        public string CurrentSection = "";

        public string CurrentLine = "";

        public bool SectionEntered;

        public bool ReaderClosed;

        public void InitINIReader(string path)
        {
            streamReader = new StreamReader(path);
        }

        public void InitINIReader_FromStream(Stream stream)
        {
            streamReader = new StreamReader(stream);
        }

        public void ReadNextLine()
        {
            SectionEntered = false;
            if (ReaderClosed || streamReader == null)
            {
                ReaderClosed = true;
                return;
            }
            CurrentLine = streamReader.ReadLine();
            if (CurrentLine == null)
            {
                CloseINIReader();
            }
            else
            {
                if (CurrentLine.StartsWith(";"))
                {
                    ReadNextLine();
                    return;
                }
                if (CurrentLine.StartsWith("["))
                {
                    SetCurrentSection(CurrentLine);
                    return;
                }
            }
            if (CurrentLine.IndexOf(";") != -1)
            {
                CurrentLine = CurrentLine.Remove(CurrentLine.IndexOf(";"));
            }
            CurrentLine = CurrentLine.Trim();
            try
            {
                if (streamReader.EndOfStream)
                {
                    CloseINIReader();
                }
            }
            catch
            {
                CloseINIReader();
            }
        }

        public string GetValue3()
        {
            if (CurrentLine != null && CurrentLine.Length > 0)
            {
                int num = CurrentLine.IndexOf("=");
                return CurrentLine.Substring(num + 1);
            }
            return "";
        }

        public string getCurrentKeyName()
        {
            if (CurrentLine != null && CurrentLine.Length > 0)
            {
                int num = CurrentLine.IndexOf("=");
                if (num > -1)
                {
                    return CurrentLine.Substring(0, num);
                }
                return null;
            }
            return null;
        }

        public bool isLineReadable()
        {
            if (CurrentLine != null && !SectionEntered && CurrentLine.Length > 0)
            {
                return true;
            }
            return false;
        }

        private void SetCurrentSection(string line)
        {
            SectionEntered = true;
            CurrentSection = line.Substring(1, line.Length - 2);
        }

        public void CloseINIReader()
        {
            ReaderClosed = true;
            CurrentSection = "";
            CurrentLine = "";
            streamReader.Close();
        }
    }
}