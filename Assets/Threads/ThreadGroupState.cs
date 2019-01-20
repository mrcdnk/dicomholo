
using System.Threading;
using UnityEngine;

namespace Threads
{
    /// <summary>
    /// Synchronized object to track progress of multiple threads.
    /// </summary>
    public class ThreadGroupState
    {
        private int progress;
        private int working;

        /// <summary>
        /// Set to the total amount of progress to be reached when the work is done. Not synchronized.
        /// </summary>
        public int TotalProgress { get; set; }
        public int Progress => progress;
        public int Working => working;

        /// <summary>
        /// Resets the state.
        /// </summary>
        public void Reset()
        {
            progress = 0;
            working = 0;
        }

        /// <summary>
        /// Used inside a thread to indicate progression.
        /// </summary>
        public void IncrementProgress()
        {
            Interlocked.Increment(ref progress);
        }

        /// <summary>
        /// Registers a thread to this state.
        /// </summary>
        public void Register()
        {
            Interlocked.Increment(ref working);
        }

        /// <summary>
        /// Called by a thread when there is no work left to do.
        /// </summary>
        public void Done()
        {
            Interlocked.Decrement(ref working);
        }
    }

}
