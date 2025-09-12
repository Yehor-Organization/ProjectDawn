using System.Collections.Concurrent;

namespace ProjectDawnApi
{
    public static class TransformationQueue
    {
        // Dictionary-like queue: always keeps the latest update per (FarmId, PlayerId)
        public static readonly ConcurrentDictionary<(int FarmId, int PlayerId), TransformationDataModel> Queue
            = new ConcurrentDictionary<(int, int), TransformationDataModel>();
    }
}
