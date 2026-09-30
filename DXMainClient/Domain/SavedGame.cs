using ClientCore;
using Rampastring.Tools;
using System;
using System.Buffers.Binary;
using System.IO;
using OpenMcdf;
using System.Diagnostics;

namespace DTAClient.Domain
{
    /// <summary>
    /// A single-player saved game.
    /// </summary>
    public class SavedGame
    {
        const string SAVED_GAME_PATH = "Saved Games/";
        const int MAX_SCENARIO_DESCRIPTION_BYTES = 1024 * 1024;

        public SavedGame(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; private set; }
        public string GUIName { get; private set; }
        public DateTime LastModified { get; private set; }
        public int CustomMissionID { get; private set; }

        /// <summary>
        /// Reads and sets the saved game's name and last modified date, and returns true if succesful.
        /// </summary>
        /// <returns>True if parsing the info was succesful, otherwise false.</returns>
        public bool ParseInfo()
        {
            try
            {
                FileInfo savedGameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAME_PATH, FileName);

                using (Stream file = savedGameFileInfo.Open(FileMode.Open, FileAccess.Read))
                using (RootStorage root = RootStorage.Open(file))
                {
                    using (CfbStream scenarioDescStream = root.OpenStream("Scenario Description"))
                    {
                        if (scenarioDescStream.Length > MAX_SCENARIO_DESCRIPTION_BYTES)
                            throw new InvalidDataException($"Scenario Description stream was unexpectedly large: {scenarioDescStream.Length} bytes.");

                        int scenarioDescLength = checked((int)scenarioDescStream.Length);
                        byte[] scenarioDescData = new byte[scenarioDescLength];
                        int bytesRead = 0;
                        while (bytesRead < scenarioDescLength)
                        {
                            int readCount = scenarioDescStream.Read(scenarioDescData, bytesRead, scenarioDescLength - bytesRead);
                            if (readCount == 0)
                                throw new EndOfStreamException("Unexpected end of stream while reading Scenario Description.");

                            bytesRead += readCount;
                        }

                        GUIName = System.Text.Encoding.Unicode.GetString(scenarioDescData).TrimEnd(['\0']);
                    }

                    if (root.TryOpenStream("CustomMissionID", out CfbStream? customMissionIdStream))
                    {
                        using (customMissionIdStream)
                        {
                            byte[] customMissionIdData = new byte[sizeof(int)];
                            int bytesRead = customMissionIdStream.Read(customMissionIdData, 0, customMissionIdData.Length);
                            CustomMissionID = bytesRead < customMissionIdData.Length
                                ? throw new EndOfStreamException("Unexpected end of stream while reading CustomMissionID.")
                                : BinaryPrimitives.ReadInt32LittleEndian(customMissionIdData);
                        }
                    }
                    else
                    {
                        CustomMissionID = 0;
                    }
                }

                LastModified = savedGameFileInfo.LastWriteTime;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while parsing saved game " + FileName + ":" +
                    ex.ToString());
                return false;
            }
        }
    }
}
