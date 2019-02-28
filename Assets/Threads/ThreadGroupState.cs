
using System.Threading;

namespace Threads
{
    /// <summary>
    /// Synchronized object to track progress of multiple threads.
    /// </summary>
    public class ThreadGroupState
    {
        /// <summary>
        /// Set to the total amount of progress to be reached when the work is done. Not synchronized.
        /// </summary>
        public int TotalProgress { get; set; }
        public int Progress => _progress;
        public int Working => _working;


        private int _progress;
        private int _working;
 
        /// <summary>
        /// Resets the state.
        /// </summary>
        public void Reset()
        {
            _progress = 0;
            _working = 0;
            TotalProgress = 0;
        }

        /// <summary>
        /// Used inside a thread to indicate progression.
        /// </summary>
        public void IncrementProgress()
        {
            Interlocked.Increment(ref _progress);
        }

        /// <summary>
        /// Registers a thread to this state.
        /// </summary>
        public void Register()
        {
            Interlocked.Increment(ref _working);
        }

        /// <summary>
        /// Called by a thread when there is no work left to do.
        /// </summary>
        public void Done()
        {
            Interlocked.Decrement(ref _working);
        }
    }

}
