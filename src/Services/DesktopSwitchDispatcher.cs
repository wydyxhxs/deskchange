using System;
using System.Collections.Generic;
using System.Threading;

namespace DeskChange.Services
{
    internal sealed class DesktopSwitchCompletedEventArgs : EventArgs
    {
        public DesktopSwitchCompletedEventArgs(
            int desktopIndex,
            bool succeeded,
            int availableDesktopCount,
            string message)
        {
            DesktopIndex = desktopIndex;
            Succeeded = succeeded;
            AvailableDesktopCount = availableDesktopCount;
            Message = message;
        }

        public int AvailableDesktopCount { get; private set; }

        public int DesktopIndex { get; private set; }

        public string Message { get; private set; }

        public bool Succeeded { get; private set; }
    }

    internal sealed class DesktopSwitchDispatcher : IDisposable
    {
        private readonly Queue<DesktopSwitchRequest> _pendingRequests = new Queue<DesktopSwitchRequest>();
        private readonly object _syncRoot = new object();
        private readonly IVirtualDesktopSwitcher _switcher;

        private bool _disposed;
        private bool _isProcessing;

        public DesktopSwitchDispatcher(IVirtualDesktopSwitcher switcher)
        {
            if (switcher == null)
            {
                throw new ArgumentNullException("switcher");
            }

            _switcher = switcher;
        }

        public event EventHandler<DesktopSwitchCompletedEventArgs> SwitchCompleted;

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _disposed = true;
                _pendingRequests.Clear();
            }
        }

        public void Enqueue(int desktopIndex, bool enableAnimation)
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _pendingRequests.Enqueue(new DesktopSwitchRequest(desktopIndex, enableAnimation));

                if (_isProcessing)
                {
                    return;
                }

                _isProcessing = true;
            }

            ThreadPool.QueueUserWorkItem(ProcessQueue);
        }

        private void Execute(DesktopSwitchRequest request)
        {
            int availableDesktopCount = _switcher.GetDesktopCount();

            if (request.DesktopIndex >= availableDesktopCount)
            {
                OnSwitchCompleted(
                    new DesktopSwitchCompletedEventArgs(
                        request.DesktopIndex,
                        false,
                        availableDesktopCount,
                        string.Format(
                            "Windows 当前只有 {0} 个虚拟桌面，桌面 {1} 暂时无法切换。",
                            availableDesktopCount,
                            request.DesktopIndex + 1)));
                return;
            }

            _switcher.SwitchToDesktop(request.DesktopIndex, request.EnableAnimation);
            OnSwitchCompleted(
                new DesktopSwitchCompletedEventArgs(
                    request.DesktopIndex,
                    true,
                    availableDesktopCount,
                    string.Empty));
        }

        private void OnSwitchCompleted(DesktopSwitchCompletedEventArgs e)
        {
            EventHandler<DesktopSwitchCompletedEventArgs> handler = SwitchCompleted;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void ProcessQueue(object state)
        {
            while (true)
            {
                DesktopSwitchRequest request;

                lock (_syncRoot)
                {
                    if (_disposed || _pendingRequests.Count == 0)
                    {
                        _isProcessing = false;
                        return;
                    }

                    request = _pendingRequests.Dequeue();
                }

                try
                {
                    Execute(request);
                }
                catch (Exception ex)
                {
                    AppLog.Error("Desktop switch failed.", ex);
                    OnSwitchCompleted(
                        new DesktopSwitchCompletedEventArgs(
                            request.DesktopIndex,
                            false,
                            -1,
                            "切换桌面失败：" + ex.Message));
                }
            }
        }

        private sealed class DesktopSwitchRequest
        {
            public DesktopSwitchRequest(int desktopIndex, bool enableAnimation)
            {
                DesktopIndex = desktopIndex;
                EnableAnimation = enableAnimation;
            }

            public int DesktopIndex { get; private set; }

            public bool EnableAnimation { get; private set; }
        }
    }
}
