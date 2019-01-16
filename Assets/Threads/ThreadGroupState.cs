
using UnityEngine;

namespace Threads
{
    /// <summary>
    /// Synchronized object to track progress of multiple threads.
    /// </summary>
    public class ThreadGroupState
    {
        private object wMutex = new object();
        private object pMutex = new object();

        public int progress;
        public int working;

        /// <summary>
        /// Set to the total amount of progress to be reached when the work is done. Not synchronized.
        /// </summary>
        public int TotalProgress { get; set; }

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
            lock (pMutex)
            {
                progress++;
            }
        }

        /// <summary>
        /// Registers a thread to this state.
        /// </summary>
        public void Register()
        {
            lock (wMutex)
            {
                working++;
            }
        }

        /// <summary>
        /// Called by a thread when there is no work left to do.
        /// </summary>
        public void Done()
        {
            lock (wMutex)
            {
                working--;
            }
        }
    }

}
