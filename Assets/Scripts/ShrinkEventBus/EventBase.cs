using System;
using System.Reflection;

namespace ShrinkEventBus
{
    public abstract class EventBase
    {
        private bool _canceled;
        private EventResult _result = EventResult.DEFAULT;
        private readonly bool _cancelable;
        private readonly bool _hasResult;
        private EventPriority? _phase;
        private readonly ListenerList _listenerList;

        public EventHandlerInfo? CurrentHandler { get; internal set; }
        public DateTime EventTime { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();

        protected EventBase()
        {
            _cancelable = GetType().GetCustomAttribute<CancelableAttribute>() != null;
            _hasResult = GetType().GetCustomAttribute<HasResultAttribute>() != null;
            _listenerList = new ListenerList();
            Setup();
        }

        protected virtual void Setup()
        {
        }

        public bool IsCancelable => _cancelable;
        public bool HasResult => _hasResult;

        public bool IsCanceled
        {
            get => _canceled;
            set
            {
                if (!_cancelable)
                    throw new UnsupportedOperationException($"事件 {GetType().Name} 不支持取消操作");
                _canceled = value;
            }
        }

        public EventResult Result
        {
            get => _result;
            set
            {
                if (!_hasResult)
                    throw new InvalidOperationException($"事件 {GetType().Name} 不支持结果设置");
                _result = value;
            }
        }

        public EventPriority? Phase => _phase;

        internal void SetPhase(EventPriority value)
        {
            if (_phase == value) return;
            if (_phase != null && _phase.Value.CompareTo(value) > 0)
                throw new ArgumentException($"尝试将事件阶段设置为 {value}，但当前已经是 {_phase}");
            _phase = value;
        }

        public void SetCanceled(bool canceled) => IsCanceled = canceled;
        public void SetResult(EventResult result) => Result = result;
        public ListenerList GetListenerList() => _listenerList;
        public EventHandlerInfo[] GetSubscribers() => _listenerList.GetAllHandlers();

        public string GetEventDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"事件类型: {GetType().FullName}");
            sb.AppendLine($"事件ID: {EventId}");
            sb.AppendLine($"触发时间: {EventTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
            sb.AppendLine($"可取消: {IsCancelable}");
            sb.AppendLine($"有结果: {HasResult}");
            sb.AppendLine($"已取消: {IsCanceled}");
            sb.AppendLine($"结果: {Result}");
            sb.AppendLine($"当前阶段: {Phase}");

            if (CurrentHandler != null)
                sb.AppendLine($"当前处理程序: {CurrentHandler.DeclaringType?.Name}.{CurrentHandler.MethodName}");

            sb.AppendLine($"订阅者数量: {_listenerList.Count}");
            sb.AppendLine("订阅者详情:");
            sb.Append(_listenerList.GetDebugInfo());

            return sb.ToString();
        }
    }
}