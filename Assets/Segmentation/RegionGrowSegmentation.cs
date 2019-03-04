using System;
using System.Collections.Generic;
using System.Threading;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class RegionGrowSegmentation : SegmentationStrategy<RegionGrowSegmentation.RegionGrowParameter>
    {
        private readonly List<Thread> _runningThreads = new List<Thread>(1); 

        /// <summary>
        /// Contains all parameters for creating a region grow segmentation
        /// </summary>
        public sealed class RegionGrowParameter
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public int Threshold { get; set; }

            public RegionGrowParameter(int x, int y, int z, int threshold = 0)
            {
                X = x;
                Y = y;
                Z = z;
                Threshold = threshold;
            }

            public bool IsValid()
            {
                return X >= 0 && Y >= 0 && Z >= 0 && Threshold >= 0;
            }
        }

        /// <summary>
        /// Kill all remaining threads if this Object gets destroyed.
        /// </summary>
        ~RegionGrowSegmentation()
        {
            foreach (var runningThread in _runningThreads)
            {
                runningThread.Abort();
            }
        }

        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Region grow parameters for the segmentation</param>
        /// <returns>The ThreadGroupState to enable progress monitoring and callback on finish. May return null if previous work has not yet been finished.</returns>
        public override ThreadGroupState Fit(Segment segment, int[] data, RegionGrowParameter parameters)
        {
            if (segment._currentWorkload.Working > 0)
            {
                return null;
            }

            segment._currentWorkload.Reset();
            segment._currentWorkload.TotalProgress = 1;
            segment._currentWorkload.Register();

            var t = new Thread(() => StartRegionGrow(segment, data, parameters))
            {
                IsBackground = true
            };
            _runningThreads.Add(t);

            t.Start();

            return segment._currentWorkload;
        }

        /// <summary>
        /// Region grow implementation
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="regionGrowParameter">Region grow parameters for the segmentation</param>
        private void StartRegionGrow(Segment segment, IReadOnlyList<int> data,
            RegionGrowParameter regionGrowParameter)
        {
            var pending = new Queue<Voxel>(20000);
            var visited = new Segment(Color.clear);
            visited.Allocate(segment.Width, segment.Height, segment.Slices);

            var seedVoxel = new Voxel(regionGrowParameter.X, regionGrowParameter.Y, regionGrowParameter.Z);

            pending.Enqueue(seedVoxel);
            visited.Set(seedVoxel.X, seedVoxel.Y, seedVoxel.Z);
            segment.Set(seedVoxel.X, seedVoxel.Y, seedVoxel.Z);

            var intensityBase = data[GetIndex(seedVoxel, segment.Width, segment.Height)];

            var intensityLower = intensityBase - regionGrowParameter.Threshold;
            var intensityUpper = intensityBase + regionGrowParameter.Threshold;

            long processed = 0;

            while (pending.Count > 0)
            {
                var currentVec = pending.Dequeue();

                var currentIdx = GetIndex(currentVec, segment.Width, segment.Height);

                var curVal = data[currentIdx];

                if (curVal < intensityLower || curVal > intensityUpper)
                {
                    continue;
                }

                segment.Set(currentVec.X, currentVec.Y, currentVec.Z);

                Voxel neighbor;

                if (currentVec.X > 0)
                {
                    neighbor = new Voxel(currentVec.X - 1, currentVec.Y, currentVec.Z);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }

                if (currentVec.X < segment.Width - 1)
                {
                    neighbor = new Voxel(currentVec.X + 1, currentVec.Y, currentVec.Z);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }

                if (currentVec.Y > 0)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y - 1, currentVec.Z);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }

                if (currentVec.Y < segment.Height - 1)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y + 1, currentVec.Z);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }
                if (currentVec.Z > 0)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y, currentVec.Z - 1);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }
                if (currentVec.Z < segment.Slices - 1)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y, currentVec.Z + 1);

                    if (!visited.Contains(neighbor.X, neighbor.Y, neighbor.Z))
                    {
                        pending.Enqueue(neighbor);
                        visited.Set(neighbor.X, neighbor.Y, neighbor.Z);
                    }
                }

                if (processed % 40000 == 0)
                {
                    Thread.Sleep(50);
                }

                processed++;
            }

            segment._currentWorkload.IncrementProgress();
            segment._currentWorkload.Done();
            _runningThreads.Remove(Thread.CurrentThread);
        }

        /// <summary>
        /// Converts the given Voxel to a 1D index.
        /// </summary>
        /// <param name="coordinates">target 3D coordinates</param>
        /// <param name="width">width of a slice</param>
        /// <param name="height">height of a slice</param>
        /// <returns>A 1D Index</returns>
        private static int GetIndex(Voxel coordinates, int width, int height)
        {
            return coordinates.Z * width * height
                   + coordinates.Y * width
                   + coordinates.X;
        }

        /// <summary>
        /// Container for 3D coordinates
        /// </summary>
        private class Voxel
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public Voxel(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

        }
    }
}
