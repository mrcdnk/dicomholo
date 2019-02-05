using Threads;

namespace Segmentation
{
    public abstract class SegmentationStrategy<TP>
    {
        public abstract ThreadGroupState Fit(Segment segment, int[] data, TP parameters);
    }
}
