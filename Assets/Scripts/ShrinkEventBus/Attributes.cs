using System;

namespace ShrinkEventBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CancelableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class HasResultAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class EventBusSubscriberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class EventSubscribeAttribute : Attribute
    {
        public EventPriority Priority { get; set; }
        public bool ReceiveCanceled { get; set; }
        public int NumericPriority { get; set; }

        public EventSubscribeAttribute()
        {
            Priority = EventPriority.NORMAL;
            ReceiveCanceled = false;
            NumericPriority = 0;
        }

        public EventSubscribeAttribute(EventPriority priority, bool receiveCanceled = false)
        {
            Priority = priority;
            ReceiveCanceled = receiveCanceled;
            NumericPriority = 0;
        }

        public EventSubscribeAttribute(int priority)
        {
            NumericPriority = priority;
            Priority = ConvertToEventPriority(priority);
            ReceiveCanceled = false;
        }

        private static EventPriority ConvertToEventPriority(int numericPriority)
        {
            return numericPriority switch
            {
                >= 100 => EventPriority.HIGHEST,
                >= 50 => EventPriority.HIGH,
                > 0 => EventPriority.NORMAL,
                >= -50 => EventPriority.LOW,
                _ => EventPriority.LOWEST
            };
        }
    }
}