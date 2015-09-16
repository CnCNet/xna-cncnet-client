using System;
using System.IO;
using System.Collections.Generic;
using dtasetup.gui;

namespace dtasetup.domain.cncnet
{
    public class CSVParser
    {
        public List<string> DataFields = new List<string>();
        public List<List<string>> DataLines = new List<List<string>>();

        public void ParseCsv(string path)
        {
            StreamReader streamReader = new StreamReader(path);
            bool dataFieldsParsed = false;

            //Logger.Log("Parsing data fields..");
            while (!dataFieldsParsed)
            {
                if (streamReader.EndOfStream)
                    return;

                string line = streamReader.ReadLine();
                if (line != String.Empty)
                {
                    DataFields = ParseLine(line);
                    dataFieldsParsed = true;
                }
            }

            //Logger.Log("Data fields parsed, parsing other lines..");

            //for (int fieldId = 0; fieldId < DataFields.Count; fieldId++)
            //{
            //    Logger.Log("Field " + fieldId + ": " + DataFields[fieldId]);
            //}

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();

                if (line != String.Empty)
                {
                    DataLines.Add(ParseLine(line));
                }
            }

            streamReader.Close();
        }

        public string getValue(int lineId, string fieldName)
        {
            for (int fieldId = 0; fieldId < DataFields.Count; fieldId++)
            {
                if (DataFields[fieldId] == fieldName)
                {
                    return DataLines[lineId][fieldId];
                }
            }

            return null;
        }

        private List<string> ParseLine(string line)
        {
            //Logger.Log("ParseLine(" + line + ")");
            List<string> returnValue = new List<string>();
            int currPos = 0;
            bool entirelyParsed = false;

            while (!entirelyParsed)
            {
                if (line.Substring(currPos, 1) == "\"")
                {
                    int startPos = currPos + 1;
                    int numChars = line.Substring(startPos).IndexOf('"');
                    if (numChars == 0)
                    {
                        numChars = line.Substring(startPos + 1).IndexOf('"');
                        numChars = line.Substring(numChars + 1).IndexOf('"');
                    }
                    returnValue.Add(line.Substring(startPos, numChars));
                    currPos = currPos + numChars + 2;
                }
                else if (line.Substring(currPos, 1) == "," ||
                    line.Substring(currPos, 1) == ";")
                {
                    int startPos = currPos + 1;
                    int numChars = line.Substring(startPos).IndexOf(',');
                    if (numChars == -1)
                        numChars = line.Substring(startPos).IndexOf(';');

                    if (numChars > -1)
                    {
                        returnValue.Add(line.Substring(startPos, numChars));
                        currPos = currPos + numChars + 1;
                    }
                    else
                    {
                        // get all remaining characters
                        returnValue.Add(line.Substring(startPos, line.Length - startPos));
                        entirelyParsed = true;
                    }
                }
                else
                {
                    int numChars = line.Substring(currPos).IndexOf(','); ;
                    if (numChars == -1)
                        numChars = line.Substring(currPos).IndexOf(';');

                    if (numChars > -1)
                    {
                        returnValue.Add(line.Substring(currPos, numChars));
                        currPos = currPos + numChars + 1;
                    }
                    else
                    {
                        // get all remaining characters
                        returnValue.Add(line.Substring(currPos, line.Length - currPos));
                        entirelyParsed = true;
                    }
                }
            }

            return returnValue;
        }
    }
}