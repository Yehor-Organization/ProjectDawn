using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi
{
    [Owned]
    public class TransformationDM
    {
        public float positionX { get; set; }
        public float positionY { get; set; }
        public float positionZ { get; set; }

        public float rotationX { get; set; }
        public float rotationY { get; set; }
        public float rotationZ { get; set; }

        // ✅ AUTHORITATIVE SERVER TIMESTAMP (seconds)
        public float serverTime { get; set; }
    }
}