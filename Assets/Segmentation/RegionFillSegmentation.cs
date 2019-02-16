﻿using System;
using System.Collections.Generic;
using System.Threading;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class RegionFillSegmentation : SegmentationStrategy<RegionFillSegmentation.RegionFillParameter>
    {
        private readonly List<Thread> _runningThreads = new List<Thread>(1); 

        public sealed class RegionFillParameter
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public int Threshold { get; set; }

            public RegionFillParameter(int x, int y, int z, int threshold = 0)
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

        ~RegionFillSegmentation()
        {
            foreach (var runningThread in _runningThreads)
            {
                runningThread.Abort();
            }
        }

        public override ThreadGroupState Fit(Segment segment, int[] data, RegionFillParameter parameters)
        {
            if (segment._currentWorkload.Working > 0)
            {
                return null;
            }

            segment._currentWorkload.Reset();
            segment._currentWorkload.TotalProgress = 1;
            segment._currentWorkload.Register();

            var t = new Thread(() => StartRegionFill(segment, data, parameters))
            {
                IsBackground = true
            };
            _runningThreads.Add(t);

            t.Start();

            return segment._currentWorkload;
        }

        private void StartRegionFill(Segment segment, IReadOnlyList<int> data,
            RegionFillParameter regionFillParameter)
        {
            var pending = new Queue<Voxel>(20000);
            var visited = new HashSet<Voxel>();

            var seedVoxel = new Voxel(regionFillParameter.X, regionFillParameter.Y, regionFillParameter.Z);

            pending.Enqueue(seedVoxel);
            visited.Add(seedVoxel);
            segment.Set(seedVoxel.X, seedVoxel.Y, seedVoxel.Z);

            var intensityBase = data[GetIndex(seedVoxel, segment.Width, segment.Height)];

            var intensityLower = intensityBase - regionFillParameter.Threshold;
            var intensityUpper = intensityBase + regionFillParameter.Threshold;

            long processed = 0;

            while (pending.Count > 0)
            {
                var currentVec = pending.Dequeue();

                var currentIdx = GetIndex(currentVec, segment.Width, segment.Height);

                if (data[currentIdx] < intensityLower || data[currentIdx] > intensityUpper)
                {
                    continue;
                }

                segment.Set(currentVec.X, currentVec.Y, currentVec.Z);

                Voxel neighbor;

                if (currentVec.X > 0)
                {
                    neighbor = new Voxel(currentVec.X - 1, currentVec.Y, currentVec.Z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }

                if (currentVec.X < segment.Width - 1)
                {
                    neighbor = new Voxel(currentVec.X + 1, currentVec.Y, currentVec.Z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }

                if (currentVec.Y > 0)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y - 1, currentVec.Z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }

                if (currentVec.Y < segment.Height - 1)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y + 1, currentVec.Z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
                if (currentVec.Z > 0)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y, currentVec.Z - 1);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
                if (currentVec.Z < segment.Slices - 1)
                {
                    neighbor = new Voxel(currentVec.X, currentVec.Y, currentVec.Z + 1);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }

                if (processed % 40000 == 0)
                {
                    Thread.Sleep(60);
                }

                processed++;
            }

            segment._currentWorkload.IncrementProgress();
            segment._currentWorkload.Done();
            _runningThreads.Remove(Thread.CurrentThread);
        }

        private int GetIndex(Voxel coordinates, int width, int height)
        {
            return coordinates.Z * width * height
                   + coordinates.X * height
                   + coordinates.Y;
        }

        private class Voxel : IEquatable<Voxel>
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

            public override bool Equals(System.Object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Voxel) obj);
            }

            
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = X;
                    hashCode = (hashCode * 397) ^ Y;
                    hashCode = (hashCode * 397) ^ Z;
                    return hashCode;
                }
            }

            public bool Equals(Voxel other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public static bool operator ==(Voxel left, Voxel right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Voxel left, Voxel right)
            {
                return !Equals(left, right);
            }
        }
    }
}
