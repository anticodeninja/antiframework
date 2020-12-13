// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public enum EventLoopStates
    {
        Idle,
        Wait,
        Active,
        Stopping,
    }

    public class EventLoop<T>
    {
        #region Fields

        private readonly Action<T> _handler;

        private readonly ConcurrentQueue<T> _queue;

        private AutoResetEvent _dedicatedStartEvent;

        private Thread _dedicatedThread;

        private int _pooledTasksCount;

        private int _active;

        private bool _dedicated;

        private bool _discardRemain;

        #endregion Fields

        #region Properties

        public ILogger Logger { get; set; }

        public bool Dedicated
        {
            get => _dedicated;
            set => SetConfig(ref _dedicated, value);
        }

        public bool DiscardRemain
        {
            get => _discardRemain;
            set => SetConfig(ref _discardRemain, value);
        }

        public EventLoopStates State => _active == 0
            ? _pooledTasksCount == 0 ? EventLoopStates.Idle : EventLoopStates.Stopping
            : _pooledTasksCount == 0 ? EventLoopStates.Wait : EventLoopStates.Active;

        #endregion Properties

        #region Constructors

        public EventLoop(Action<T> handler)
        {
            _handler = handler;
            _queue = new ConcurrentQueue<T>();
            _dedicated = false;
            _discardRemain = false;
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) == 1)
                return;

            if (_dedicated)
            {
                _dedicatedStartEvent = new AutoResetEvent(false);
                _dedicatedThread = new Thread(DedicatedWorker);
                _dedicatedThread.Start();
            }
            else
            {
                // Nothing to do for threadPool worker
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _active, 0, 1) == 0)
                return;

            if (_dedicated)
            {
                _dedicatedStartEvent.Set();
                _dedicatedThread.Join();
                _dedicatedStartEvent.Dispose();
            }
            else
            {
                while (_pooledTasksCount > 0)
                    Thread.Yield();
            }
        }

        public void Add(T task)
        {
            _queue.Enqueue(task);
            if (_dedicated)
            {
                if (Interlocked.Increment(ref _pooledTasksCount) == 1)
                    _dedicatedStartEvent.Set();
            }
            else
            {
                if (Interlocked.Increment(ref _pooledTasksCount) == 1)
                    ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorker, null);
            }
        }

        private void DedicatedWorker(object state)
        {
            for(;;)
            {
                _dedicatedStartEvent.WaitOne();
                if (WorkerImpl())
                    return;
            }
        }

        private void ThreadPoolWorker(object state) => WorkerImpl();

        /// <returns>True if stop is called and outer loop should be exited</returns>
        private bool WorkerImpl()
        {
            var active = true;
            for (;;)
            {
                if (_active == 0 && (_discardRemain || _pooledTasksCount == 0))
                {
                    while (_queue.TryDequeue(out _))
                    {
                    }

                    _pooledTasksCount = 0;
                    return true;
                }

                if (!active)
                    return false;

                if (!_queue.TryDequeue(out var task))
                    Logger?.Log(LogLevels.Error, () => "State inconsistency, try to dequeue from empty queue");

                try
                {
                    Logger?.Log(LogLevels.Trace, () => $"Handle event {task}");
                    _handler(task);
                }
                catch (AggregateException e)
                {
                    Logger?.Log(LogLevels.Error, () => $"Exception has been thrown {e}");
                }
                finally
                {
                    active = Interlocked.Decrement(ref _pooledTasksCount) > 0;
                }
            }
        }

        private void SetConfig<T>(ref T slot, T value)
        {
            if (_active != 0)
                throw new InvalidOperationException("Configuration cannot be changed after start");
            slot = value;
        }

        #endregion Methods
    }
}
