#nullable enable

namespace DTAClient.Domain
{
    internal sealed class SavedGameMetadata
    {
        internal SavedGameMetadata(string description, int customMissionID)
        {
            Description = description;
            CustomMissionID = customMissionID;
        }

        public string Description { get; }
        public int CustomMissionID { get; }
    }
}
