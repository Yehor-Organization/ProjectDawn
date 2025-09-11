namespace ProjectDawnApi
{
    public class FarmVisitorDataModel
    {
        public int FarmId { get; set; }
        public FarmDataModel Farm { get; set; }

        public int PlayerId { get; set; }
        public PlayerDataModel PlayerDataModel { get; set; }

        // Nullable because visitors may exist before joining SignalR
        public string? ConnectionId { get; set; }

        public float LastPositionX { get; set; }
        public float LastPositionY { get; set; }
        public float LastPositionZ { get; set; }
    }
}
