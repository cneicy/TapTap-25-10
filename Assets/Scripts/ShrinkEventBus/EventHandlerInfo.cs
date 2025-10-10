using System;
using System.Reflection;

namespace ShrinkEventBus
{
    public class EventHandlerInfo
    {
        public Delegate Handler { get; }
        public EventPriority Priority { get; }
        public int NumericPriority { get; }
        public bool ReceiveCanceled { get; }
        public object? Target { get; }
        public MethodInfo Method { get; }
        public string DebugInfo { get; }
        public Type DeclaringType { get; }
        public string MethodName { get; }
        public MethodInfo? OriginalMethod { get; }
        public string OriginalMethodName { get; }
        public Type? OriginalDeclaringType { get; }

        public EventHandlerInfo(Delegate handler, EventPriority priority, int numericPriority,
            bool receiveCanceled, string debugInfo = "", MethodInfo? originalMethod = null)
        {
            Handler = handler;
            Priority = priority;
            NumericPriority = numericPriority;
            ReceiveCanceled = receiveCanceled;
            Target = handler.Target;
            Method = handler.Method;
            DebugInfo = debugInfo;
            DeclaringType = Method.DeclaringType ?? typeof(object);
            MethodName = Method.Name;

            OriginalMethod = ExtractOriginalMethodFromWrapper(handler) ?? originalMethod;

            if (OriginalMethod != null)
            {
                OriginalMethodName = OriginalMethod.Name;
                OriginalDeclaringType = OriginalMethod.DeclaringType;
            }
            else
            {
                OriginalMethodName = MethodName;
                OriginalDeclaringType = DeclaringType;
            }
        }

        private static MethodInfo? ExtractOriginalMethodFromWrapper(Delegate handler)
        {
            if (handler.Target == null) return null;

            var targetType = handler.Target.GetType();
            if (!targetType.Name.Contains("MethodInfoPreservingWrapper")) return null;

            try
            {
                var originalMethodProperty = targetType.GetProperty("OriginalMethod");
                if (originalMethodProperty != null)
                    return originalMethodProperty.GetValue(handler.Target) as MethodInfo;
            }
            catch
            {
            }

            return null;
        }

        public string DisplayMethodName => OriginalMethodName;
        public Type DisplayDeclaringType => OriginalDeclaringType ?? DeclaringType;
    }
}