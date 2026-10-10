#nullable enable

using System.Buffers.Binary;
using System.IO;
using System.Text;

using OpenMcdf;

namespace DTAClient.Domain
{
    internal static class OleSavedGameReader
    {
        private const int MaxScenarioDescriptionBytes = 1024 * 1024;

        internal static SavedGameMetadata ReadInfo(Stream file)
        {
            using (RootStorage root = RootStorage.Open(file))
            {
                string description;

                using (CfbStream scenarioDescStream = root.OpenStream("Scenario Description"))
                {
                    if (scenarioDescStream.Length > MaxScenarioDescriptionBytes)
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

                    description = Encoding.Unicode.GetString(scenarioDescData).TrimEnd(['\0']);
                }

                int customMissionID = 0;

                if (root.TryOpenStream("CustomMissionID", out CfbStream? customMissionIdStream))
                {
                    using (customMissionIdStream)
                    {
                        byte[] customMissionIdData = new byte[sizeof(int)];
                        int bytesRead = customMissionIdStream.Read(customMissionIdData, 0, customMissionIdData.Length);
                        customMissionID = bytesRead < customMissionIdData.Length
                            ? throw new EndOfStreamException("Unexpected end of stream while reading CustomMissionID.")
                            : BinaryPrimitives.ReadInt32LittleEndian(customMissionIdData);
                    }
                }

                return new SavedGameMetadata(description, customMissionID);
            }
        }
    }
}
