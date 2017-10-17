﻿
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.SignalR.Transport
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context

    internal sealed class TaskQueue
    {
        private readonly object _lockObj = new object();
        private Task _lastQueuedTask;
        private volatile bool _drained;
        private readonly int? _maxSize;
        private long _size;

        // This is the TaskQueueMonitor in the .NET client that watches for
        // suspected deadlocks in user code.
        private readonly ITaskMonitor _taskMonitor;

        public TaskQueue()
            : this(TaskAsyncHelper.Empty)
        {
        }

        public TaskQueue(Task initialTask)
        {
            _lastQueuedTask = initialTask;
        }

        public TaskQueue(Task initialTask, int maxSize)
        {
            _lastQueuedTask = initialTask;
            _maxSize = maxSize;
        }


        public TaskQueue(Task initialTask, ITaskMonitor taskMonitor)
            : this(initialTask)
        {
            _taskMonitor = taskMonitor;
        }

        public bool IsDrained
        {
            get
            {
                return _drained;
            }
        }

        public Task Enqueue(Func<object, Task> taskFunc, object state)
        {
            // Lock the object for as short amount of time as possible
            lock (_lockObj)
            {
                if (_drained)
                {
                    return _lastQueuedTask;
                }

                if (_maxSize != null)
                {
                    // Increment the size if the queue
                    if (Interlocked.Increment(ref _size) > _maxSize)
                    {
                        Interlocked.Decrement(ref _size);

                        // We failed to enqueue because the size limit was reached
                        return null;
                    }
                }

                var newTask = _lastQueuedTask.Then((n, ns, q) => q.InvokeNext(n, ns), taskFunc, state, this);

                _lastQueuedTask = newTask;
                return newTask;
            }
        }

        private Task InvokeNext(Func<object, Task> next, object nextState)
        {
            if (_taskMonitor != null)
            {
                _taskMonitor.TaskStarted();
            }

            return next(nextState).Finally(s => ((TaskQueue)s).Dequeue(), this);
        }

        private void Dequeue()
        {
            if (_taskMonitor != null)
            {
                _taskMonitor.TaskCompleted();
            }

            if (_maxSize != null)
            {
                // Decrement the number of items left in the queue
                Interlocked.Decrement(ref _size);
            }
        }

        public Task Enqueue(Func<Task> taskFunc)
        {
            return Enqueue(state => ((Func<Task>)state).Invoke(), taskFunc);
        }

        public Task Drain()
        {
            lock (_lockObj)
            {
                _drained = true;

                return _lastQueuedTask;
            }
        }
    }
}
