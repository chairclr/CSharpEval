using System.Collections.Concurrent;

namespace CSharpEval;

public static class SynchronousExecutor
{
    public static void Run(Func<Task> task)
    {
        SynchronizationContext? context = SynchronizationContext.Current;

        LocalSynchronizationContext synchronizationContext = new LocalSynchronizationContext();

        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        synchronizationContext.Post(async _ =>
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                synchronizationContext.InnerException = ex;
            }
            finally
            {
                synchronizationContext.Stop();
            }
        }, null);

        synchronizationContext.Run();

        SynchronizationContext.SetSynchronizationContext(context);
    }

    public static T? Run<T>(Func<Task<T>> task)
    {
        SynchronizationContext? context = SynchronizationContext.Current;

        LocalSynchronizationContext synchronizationContext = new LocalSynchronizationContext();

        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        T? result = default;
        synchronizationContext.Post(async _ =>
        {
            try
            {
                result = await task();
            }
            catch (Exception ex)
            {
                synchronizationContext.InnerException = ex;
            }
            finally
            {
                synchronizationContext.Stop();
            }
        }, null);

        synchronizationContext.Run();

        SynchronizationContext.SetSynchronizationContext(context);

        return result;
    }

    private sealed class LocalSynchronizationContext : SynchronizationContext
    {
        private sealed class Work
        {
            public readonly SendOrPostCallback Callback;

            public readonly object? State;

            public Work(SendOrPostCallback callback, object? state)
            {
                Callback = callback;
                State = state;
            }
        }

        private volatile bool Stopped;

        private readonly BlockingCollection<Work> WorkQueue = new BlockingCollection<Work>();

        public Exception? InnerException { get; set; }

        public override void Send(SendOrPostCallback work, object? state)
        {
            throw new NotSupportedException("Cannot send work to self.");
        }

        public override void Post(SendOrPostCallback work, object? state)
        {
            WorkQueue.Add(new Work(work, state));
        }

        public void Stop()
        {
            Post(_ => Stopped = true, null);
        }

        public void Run()
        {
            while (!Stopped)
            {
                if (WorkQueue.TryTake(out Work? task))
                {
                    task.Callback.Invoke(task.State);

                    if (InnerException is not null)
                    {
                        throw InnerException;
                    }
                }
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}
