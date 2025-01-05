namespace AsynchronousServer.DataType
{
    public sealed class TaskThreadPair
    {
        public Task? Task { get; set; }
        public Thread? CurrentThread { get; set; }

        public bool IsTaskCompleted => Task?.IsCompleted ?? false;
        public bool IsThreadAlive => CurrentThread?.IsAlive ?? false;

        public bool Interrupt()
        {
            if (this.CurrentThread != null && this.CurrentThread.IsAlive)
            {
                this.CurrentThread.Interrupt();
                return true;
            }
            return false;
        }

        [Obsolete("The Abort method is not recommended. Please use Interrupt whenever possible.")]
        public bool Abort()
        {
            if (this.CurrentThread != null && this.CurrentThread.IsAlive)
            {
#pragma warning disable SYSLIB0006
                this.CurrentThread.Abort();
#pragma warning restore SYSLIB0006
                return true;
            }
            return false;
        }
    }
}