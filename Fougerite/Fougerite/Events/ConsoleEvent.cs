namespace Fougerite.Events
{
    public class ConsoleEvent
    {
        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            Cancelled = true;
        }
        
        /// <summary>
        /// Gets if the console message was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get;
            private set;
        }
    }
}