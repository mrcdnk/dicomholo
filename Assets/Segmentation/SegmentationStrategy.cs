using System.Collections.Generic;
using Threads;

namespace Segmentation
{
    /// <summary>
    /// Base class for implementing custom segmentation algorithms
    /// </summary>
    /// <typeparam name="TP">The type of the parameters for the algorithm</typeparam>
    public abstract class SegmentationStrategy<TP>
    {
        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Custom parameters for the segmentation</param>
        /// <returns>The ThreadGroupState to enable progress monitoring and callback on finish. May return null if previous work has not yet been finished.</returns>
        public abstract ThreadGroupState Fit(Segment segment, int[] data, TP parameters);
    }
}
