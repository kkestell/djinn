
using Djinn.Services;

namespace Djinn.Collections;

internal class RetryableQueue<T>(TimeSpan? waitBeforeRetrying = null)
    where T : class
{
    private readonly TimeSpan _waitBeforeRetrying = waitBeforeRetrying ?? TimeSpan.FromMinutes(5);
    private readonly HashSet<T> _previouslyEnqueued = new();
    private readonly Queue<T> _retryQueue = new();
    private Queue<T> _mainQueue = new();

    public void Enqueue(T item)
    {
        if (_previouslyEnqueued.Contains(item))
        {
            _retryQueue.Enqueue(item);
        }
        else
        {
            _mainQueue.Enqueue(item);
            _previouslyEnqueued.Add(item);
        }
    }

    public void EnqueueMany(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public bool TryDequeue(out T? item)
    {
        if (_mainQueue.Count == 0 && _retryQueue.Count > 0)
        {
            Log.Verbose($"Waiting {_waitBeforeRetrying} before retrying…");
            Thread.Sleep(_waitBeforeRetrying);
            
            _mainQueue = new Queue<T>(_retryQueue);
            _retryQueue.Clear();
        }

        if (_mainQueue.TryDequeue(out item))
        {
            return true;
        }

        item = null;
        return false;
    }
}