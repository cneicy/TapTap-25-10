using System;
using System.Collections.Generic;
using System.Reflection;
#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。

namespace ShrinkEventBus
{
    public class ListenerList
    {
        private readonly List<EventHandlerInfo> _handlers = new();
        private readonly object _lock = new();
        private bool _needsSort = true;

        public void Add(Delegate handler, EventPriority priority, int numericPriority,
            bool receiveCanceled, string debugInfo = "", MethodInfo? originalMethod = null)
        {
            lock (_lock)
            {
                _handlers.Add(new EventHandlerInfo(handler, priority, numericPriority,
                    receiveCanceled, debugInfo, originalMethod));
                _needsSort = true;
            }
        }

        public bool Remove(Delegate handler)
        {
            lock (_lock)
            {
                for (var i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (!_handlers[i].Handler.Equals(handler)) continue;
                    _handlers.RemoveAt(i);
                    return true;
                }

                return false;
            }
        }

        public void RemoveTarget(object target)
        {
            lock (_lock)
            {
                for (var i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i].Target == target)
                        _handlers.RemoveAt(i);
                }
            }
        }

        public EventHandlerInfo[] GetSortedHandlers()
        {
            lock (_lock)
            {
                if (!_needsSort) return _handlers.ToArray();

                _handlers.Sort((a, b) =>
                {
                    var priorityCompare = a.Priority.CompareTo(b.Priority);
                    return priorityCompare != 0 ? priorityCompare : b.NumericPriority.CompareTo(a.NumericPriority);
                });
                _needsSort = false;

                return _handlers.ToArray();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _handlers.Count;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _needsSort = false;
            }
        }

        public EventHandlerInfo[] GetAllHandlers()
        {
            lock (_lock)
            {
                return _handlers.ToArray();
            }
        }

        public string GetDebugInfo()
        {
            lock (_lock)
            {
                var sb = new System.Text.StringBuilder();
                var sortedHandlers = GetSortedHandlers();

                for (var i = 0; i < sortedHandlers.Length; i++)
                {
                    var handler = sortedHandlers[i];
                    sb.AppendLine($"  [{i + 1}] {handler.DisplayDeclaringType?.Name}.{handler.DisplayMethodName} " +
                                  $"- Priority: {handler.Priority}({handler.NumericPriority}), " +
                                  $"ReceiveCanceled: {handler.ReceiveCanceled}");
                }

                return sb.ToString();
            }
        }
    }
}