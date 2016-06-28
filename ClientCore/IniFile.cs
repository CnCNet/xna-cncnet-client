// Rampastring's INI parser
// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Linq;

namespace ClientCore
{
    /// <summary>
    /// A class for parsing, handling and writing INI files.
    /// </summary>
    public class IniFile
    {
        #region Static methods

        public static IniFile ConsolidateIniFiles(IniFile firstIni, IniFile secondIni)
        {
            List<string> sections = secondIni.GetSections();

            foreach (string section in sections)
            {
                List<string> sectionKeys = secondIni.GetSectionKeys(section);
                foreach (string key in sectionKeys)
                {
                    firstIni.SetStringValue(section, key, secondIni.GetStringValue(section, key, "no value defined in source INI"));
                }
            }

            return firstIni;
        }

        #endregion

        public IniFile() { }

        public IniFile(string filePath)
        {
            originalFilePath = filePath;

            if (File.Exists(filePath))
            {
                ParseIniFile(File.OpenRead(filePath));
            }
        }

        public IniFile(Stream stream)
        {
            ParseIniFile(stream);
        }

        public void SetFilePath(string path)
        {
            originalFilePath = path;
        }

        string originalFilePath = String.Empty;
        List<IniSection> Sections = new List<IniSection>();

        private void ParseIniFile(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);

            int currentSectionId = -1;
            string currentLine = String.Empty;

            while (!reader.EndOfStream)
            {
                currentLine = reader.ReadLine();

                int commentStartIndex = currentLine.IndexOf(';');
                if (commentStartIndex > -1)
                    currentLine = currentLine.Substring(0, commentStartIndex);

                if (String.IsNullOrEmpty(currentLine))
                    continue;

                if (currentLine[0] == '[')
                {
                    string sectionName = currentLine.Substring(1, currentLine.IndexOf(']') - 1);
                    int index = Sections.FindIndex(c => c.SectionName == sectionName);

                    if (index > -1)
                    {
                        currentSectionId = index;
                    }
                    else
                    {
                        Sections.Add(new IniSection(sectionName));
                        currentSectionId = Sections.Count - 1;
                    }

                    continue;
                }

                if (currentSectionId == -1)
                    continue;

                int equalsIndex = currentLine.IndexOf('=');

                if (equalsIndex == -1)
                {
                    Sections[currentSectionId].AddKey(currentLine.Trim(), String.Empty);
                }
                else
                {
                    Sections[currentSectionId].AddKey(currentLine.Substring(0, equalsIndex).Trim(),
                        currentLine.Substring(equalsIndex + 1).Trim());
                }
            }

            reader.Close();
        }

        public void WriteIniFile()
        {
            WriteIniFile(originalFilePath);
        }

        public void WriteIniFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            StreamWriter sw = new StreamWriter(File.OpenWrite(filePath));
            foreach (IniSection section in Sections)
            {
                sw.WriteLine("[" + section.SectionName + "]");
                foreach (string[] key in section.Keys)
                {
                    sw.WriteLine(key[0] + "=" + key[1]);
                }
                sw.WriteLine();
            }

            sw.WriteLine();
            sw.Close();
        }

        public void MoveSectionToFirst(string sectionName)
        {
            int index = Sections.FindIndex(s => s.SectionName == sectionName);

            if (index == -1)
                return;

            IniSection section = Sections[index];

            Sections.RemoveAt(index);
            Sections.Insert(0, section);
        }

        public void EraseSectionKeys(string sectionName)
        {
            int index = Sections.FindIndex(s => s.SectionName == sectionName);

            if (index == -1)
                return;

            Sections[index].Keys.Clear();
        }

        public string GetStringValue(string section, string key, string defaultValue)
        {
            bool success = false;
            return GetStringValue(section, key, defaultValue, out success);
        }

        public string GetStringValue(string section, string key, string defaultValue, out bool success)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                success = false;
                return defaultValue;
            }
            else
            {
                string[] keyArray = Sections[sectionId].Keys.Find(c => c[0] == key);

                if (keyArray == null)
                {
                    success = false;
                    return defaultValue;
                }
                else
                {
                    success = true;
                    return keyArray[1];
                }
            }
        }

        public int GetIntValue(string section, string key, int defaultValue)
        {
            bool success = false;
            return GetIntValue(section, key, defaultValue, out success);
        }

        public int GetIntValue(string section, string key, int defaultValue, out bool success)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            success = false;

            if (sectionId == -1)
            {
                return defaultValue;
            }
            else
            {
                string[] keyArray = Sections[sectionId].Keys.Find(c => c[0] == key);

                if (keyArray == null)
                {
                    return defaultValue;
                }
                else
                {
                    try
                    {
                        success = true;
                        return Convert.ToInt32(keyArray[1]);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
        }

        public double GetDoubleValue(string section, string key, double defaultValue)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
                return defaultValue;
            else
            {
                string[] keyArray = Sections[sectionId].Keys.Find(c => c[0] == key);

                if (keyArray == null)
                    return defaultValue;
                else
                {
                    try
                    {
                        return Convert.ToDouble(keyArray[1], CultureInfo.GetCultureInfo("en-US").NumberFormat);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
        }

        public float GetSingleValue(string section, string key, float defaultValue)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
                return defaultValue;
            else
            {
                string[] keyArray = Sections[sectionId].Keys.Find(c => c[0] == key);

                if (keyArray == null)
                    return defaultValue;
                else
                {
                    try
                    {
                        return Convert.ToSingle(keyArray[1], CultureInfo.GetCultureInfo("en-US").NumberFormat);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
        }

        public bool GetBooleanValue(string section, string key, bool defaultValue)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
                return defaultValue;
            else
            {
                string[] keyArray = Sections[sectionId].Keys.Find(c => c[0] == key);

                if (keyArray == null)
                    return defaultValue;
                else
                {
                    if (String.IsNullOrEmpty(keyArray[1]))
                        return defaultValue;

                    char firstChar = keyArray[1].ToLower()[0];

                    switch (firstChar)
                    {
                        case 't':
                        case 'y':
                        case '1':
                        case 'a':
                        case 'e':
                            return true;
                        case 'n':
                        case 'f':
                        case '0':
                            return false;
                        default:
                            return defaultValue;
                    }
                }
            }
        }

        public void SetStringValue(string section, string key, string value)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                Sections.Add(new IniSection(section));
                Sections[Sections.Count - 1].Keys.Add(new string[2] { key, value });
            }
            else
            {
                int keyId = Sections[sectionId].Keys.FindIndex(c => c[0] == key);
                if (keyId == -1)
                    Sections[sectionId].Keys.Add(new string[2] { key, value });
                else
                    Sections[sectionId].Keys[keyId][1] = value;
            }
        }

        public void SetIntValue(string section, string key, int value)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                Sections.Add(new IniSection(section));
                Sections[Sections.Count - 1].Keys.Add(new string[2] { key, Convert.ToString(value) });
            }
            else
            {
                int keyId = Sections[sectionId].Keys.FindIndex(c => c[0] == key);
                if (keyId == -1)
                    Sections[sectionId].Keys.Add(new string[2] { key, Convert.ToString(value) });
                else
                    Sections[sectionId].Keys[keyId][1] = Convert.ToString(value);
            }
        }

        public void SetDoubleValue(string section, string key, double value)
        {
            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                Sections.Add(new IniSection(section));
                Sections[Sections.Count - 1].Keys.Add(new string[2] { key, Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat) });
            }
            else
            {
                int keyId = Sections[sectionId].Keys.FindIndex(c => c[0] == key);
                if (keyId == -1)
                    Sections[sectionId].Keys.Add(new string[2] { key, Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat) });
                else
                    Sections[sectionId].Keys[keyId][1] = Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat);
            }
        }

        public void SetSingleValue(string section, string key, float value)
        {
            SetSingleValue(section, key, value, 0);
        }

        public void SetSingleValue(string section, string key, double value, int decimals)
        {
            SetSingleValue(section, key, Convert.ToSingle(value), decimals);
        }

        public void SetSingleValue(string section, string key, float value, int decimals)
        {
            string strValue = Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat);

            if (decimals > 0)
            {
                strValue = strValue + ".";
                for (int dc = 0; dc < decimals; dc++)
                    strValue = strValue + "0";
            }

            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                Sections.Add(new IniSection(section));
                Sections[Sections.Count - 1].Keys.Add(new string[2] { key, Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat) });
            }
            else
            {
                int keyId = Sections[sectionId].Keys.FindIndex(c => c[0] == key);
                if (keyId == -1)
                    Sections[sectionId].Keys.Add(new string[2] { key, Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat) });
                else
                    Sections[sectionId].Keys[keyId][1] = Convert.ToString(value, CultureInfo.GetCultureInfo("en-US").NumberFormat);
            }
        }

        public void SetBooleanValue(string section, string key, bool value)
        {
            string strValue;
            if (value)
                strValue = "Yes";
            else
                strValue = "No";

            int sectionId = Sections.FindIndex(c => c.SectionName == section);
            if (sectionId == -1)
            {
                Sections.Add(new IniSection(section));
                Sections[Sections.Count - 1].Keys.Add(new string[2] { key, strValue });
            }
            else
            {
                int keyId = Sections[sectionId].Keys.FindIndex(c => c[0] == key);
                if (keyId == -1)
                    Sections[sectionId].Keys.Add(new string[2] { key, strValue });
                else
                    Sections[sectionId].Keys[keyId][1] = strValue;
            }
        }

        /// <summary>
        /// Gets the names of all INI keys in the specified INI section.
        /// </summary>
        public List<string> GetSectionKeys(string sectionName)
        {
            IniSection section = Sections.Find(c => c.SectionName == sectionName);

            if (section == null)
                return null;
            else
            {
                List<string> returnValue = new List<string>();

                foreach (string[] key in section.Keys)
                    returnValue.Add(key[0]);

                return returnValue;
            }
        }

        /// <summary>
        /// Gets the names of all sections in the INI file.
        /// </summary>
        public List<string> GetSections()
        {
            List<string> sectionList = new List<string>();

            foreach (IniSection section in Sections)
                sectionList.Add(section.SectionName);

            return sectionList;
        }

        public bool SectionExists(string sectionName)
        {
            int index = Sections.FindIndex(c => c.SectionName == sectionName);

            if (index == -1)
                return false;

            return true;
        }
    }

    public class IniSection
    {
        public IniSection() { }

        public IniSection(string sectionName)
        {
            SectionName = sectionName;
        }

        public string SectionName { get; set; }
        public List<string[]> Keys = new List<string[]>();

        public void AddKey(string keyName, string value)
        {
            string[] key = new string[2];
            key[0] = keyName;
            key[1] = value;

            Keys.Add(key);
        }
    }
}
