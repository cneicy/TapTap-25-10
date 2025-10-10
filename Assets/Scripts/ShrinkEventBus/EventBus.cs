using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace ShrinkEventBus
{
    /// <summary>
    /// 事件总线 - Unity版本
    /// </summary>
    /// <remarks>
    /// 提供基于事件类型的发布-订阅模式实现
    /// 支持类型安全的事件处理和自动注册
    /// </remarks>
    public static class EventBus
    {
        // 存储所有已注册的事件处理程序
        private static readonly Dictionary<Type, ListenerList> EventHandlers = new();

        // 存储已经注册过的实例，避免重复注册
        private static readonly HashSet<object> RegisteredInstances = new();

        // 线程安全锁
        private static readonly object InstanceLock = new();
        private static readonly object AutoRegInitLock = new();

        // 标记是否已确保自动注册管理器初始化
        private static bool _hasEnsuredAutoRegInit;

        /// <summary>
        /// 静态构造函数，初始化时注册静态事件处理程序
        /// </summary>
        static EventBus()
        {
            EventBusRegHelper.RegStaticEventHandler();
        }

        /// <summary>
        /// 确保自动注册管理器已初始化
        /// </summary>
        private static void EnsureAutoManagerInitialized()
        {
            if (_hasEnsuredAutoRegInit) return;

            lock (AutoRegInitLock)
            {
                if (_hasEnsuredAutoRegInit) return;
                EventAutoRegHelper.EnsureInitialized();
                _hasEnsuredAutoRegInit = true;
            }
        }

        #region 事件注册 API

        /// <summary>
        /// 注册同步事件处理程序（带方法信息）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<TEvent>(Action<TEvent> handler, string methodName, Type declaringType,
            EventPriority priority = EventPriority.NORMAL, bool receiveCanceled = false) where TEvent : EventBase
        {
            var methodInfo = declaringType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            Func<TEvent, Task> asyncHandler = arg =>
            {
                handler(arg);
                return Task.CompletedTask;
            };

            RegisterEventInternal(typeof(TEvent), asyncHandler, priority, 0, receiveCanceled,
                $"Manual Sync Handler (Priority: {priority})", methodInfo);
        }

        /// <summary>
        /// 注册异步事件处理程序（数字优先级）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<TEvent>(Func<TEvent, Task> handler, int priority = 0) where TEvent : EventBase
        {
            var eventPriority = ConvertToEventPriority(priority);
            RegisterEventInternal(typeof(TEvent), handler, eventPriority, priority, false,
                $"Manual Async Handler (Priority: {priority})");
        }

        /// <summary>
        /// 注册同步事件处理程序（枚举优先级）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<TEvent>(Action<TEvent> handler, EventPriority priority = EventPriority.NORMAL,
            bool receiveCanceled = false) where TEvent : EventBase
        {
            Func<TEvent, Task> asyncHandler = arg =>
            {
                handler(arg);
                return Task.CompletedTask;
            };

            RegisterEventInternal(typeof(TEvent), asyncHandler, priority, 0, receiveCanceled,
                $"Manual Sync Handler (Priority: {priority})");
        }

        /// <summary>
        /// 注册同步事件处理程序（数字优先级）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : EventBase
        {
            var eventPriority = ConvertToEventPriority(priority);
            Func<TEvent, Task> asyncHandler = arg =>
            {
                handler(arg);
                return Task.CompletedTask;
            };

            RegisterEventInternal(typeof(TEvent), asyncHandler, eventPriority, priority, false,
                $"Manual Sync Handler (Priority: {priority})");
        }

        /// <summary>
        /// 注册异步事件处理程序（枚举优先级）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<TEvent>(Func<TEvent, Task> handler,
            EventPriority priority = EventPriority.NORMAL,
            bool receiveCanceled = false) where TEvent : EventBase
        {
            RegisterEventInternal(typeof(TEvent), handler, priority, 0, receiveCanceled,
                $"Manual Async Handler (Priority: {priority})");
        }

        /// <summary>
        /// 将数字优先级转换为枚举优先级
        /// </summary>
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

        /// <summary>
        /// 内部事件注册方法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RegisterEventInternal(Type eventType, Delegate handler, EventPriority priority,
            int numericPriority, bool receiveCanceled, string debugInfo = "", MethodInfo? originalMethod = null)
        {
            lock (EventHandlers)
            {
                if (!EventHandlers.TryGetValue(eventType, out var collection))
                {
                    collection = new ListenerList();
                    EventHandlers[eventType] = collection;
                }

                collection.Add(handler, priority, numericPriority, receiveCanceled, debugInfo, originalMethod);
            }
        }

        #endregion

        #region 事件注销 API

        /// <summary>
        /// 注销异步事件处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterEvent<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase
        {
            var eventType = typeof(TEvent);
            lock (EventHandlers)
            {
                if (!EventHandlers.TryGetValue(eventType, out var collection)) return;
                collection.Remove(handler);
                if (collection.Count == 0)
                    EventHandlers.Remove(eventType);
            }
        }

        /// <summary>
        /// 注销同步事件处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterEvent<TEvent>(Action<TEvent> handler) where TEvent : EventBase
        {
            var eventType = typeof(TEvent);
            lock (EventHandlers)
            {
                if (!EventHandlers.TryGetValue(eventType, out var collection)) return;

                var handlers = collection.GetSortedHandlers();
                foreach (var handlerInfo in handlers)
                {
                    if (!IsWrappedSyncHandler(handlerInfo.Handler, handler)) continue;
                    collection.Remove(handlerInfo.Handler);
                    break;
                }

                if (collection.Count == 0)
                    EventHandlers.Remove(eventType);
            }
        }

        /// <summary>
        /// 检查是否是包装的同步处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWrappedSyncHandler(Delegate asyncHandler, Delegate targetHandler)
        {
            try
            {
                var asyncMethodInfo = asyncHandler.Method;

                if (asyncMethodInfo.Name.Contains('<'))
                {
                    var target = asyncHandler.Target;
                    if (target == null) return false;

                    var fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var field in fields)
                    {
                        if (field.FieldType != typeof(Action<>) && field.FieldType != typeof(Delegate)) continue;
                        if (field.GetValue(target) is Delegate actualDelegate &&
                            actualDelegate.Target == targetHandler.Target &&
                            actualDelegate.Method == targetHandler.Method)
                        {
                            return true;
                        }
                    }
                }
                else if (asyncHandler.Target == targetHandler.Target &&
                         asyncHandler.Method == targetHandler.Method)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// 取消指定事件类型的所有处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CancelEvent<TEvent>() where TEvent : EventBase
        {
            var eventType = typeof(TEvent);
            lock (EventHandlers)
            {
                EventHandlers.Remove(eventType);
            }
        }

        /// <summary>
        /// 注销指定对象的所有事件处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterAllEventsForObject(object targetObject)
        {
            if (targetObject is null)
            {
                if (Debug.isDebugBuild)
                    Debug.Log("目标对象为 null，无法取消订阅。");
                return;
            }

            lock (EventHandlers)
            {
                var eventTypesToRemove = new List<Type>();

                foreach (var kvp in EventHandlers)
                {
                    kvp.Value.RemoveTarget(targetObject);
                    if (kvp.Value.Count == 0)
                        eventTypesToRemove.Add(kvp.Key);
                }

                foreach (var eventType in eventTypesToRemove)
                    EventHandlers.Remove(eventType);
            }
        }

        /// <summary>
        /// 注销指定实例的所有事件处理程序并从注册列表中移除
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterInstance(object targetObject)
        {
            if (targetObject is null)
            {
                if (Debug.isDebugBuild)
                    Debug.Log("目标对象为 null，无法取消订阅。");
                return;
            }

            lock (InstanceLock)
            {
                RegisteredInstances.Remove(targetObject);
            }

            UnregisterAllEventsForObject(targetObject);
        }

        /// <summary>
        /// 注销所有事件处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterAllEvents()
        {
            lock (EventHandlers)
            {
                EventHandlers.Clear();
            }

            lock (InstanceLock)
            {
                RegisteredInstances.Clear();
            }

            if (Debug.isDebugBuild)
                Debug.Log("所有事件订阅已被注销。");
        }

        #endregion

        #region 事件触发 API

        /// <summary>
        /// 异步触发指定类型的事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<bool> TriggerEventAsync<TEvent>(TEvent eventArgs) where TEvent : EventBase
        {
            EnsureAutoManagerInitialized();
            var eventType = typeof(TEvent);

            ListenerList? collection;
            lock (EventHandlers)
            {
                if (!EventHandlers.TryGetValue(eventType, out collection))
                {
                    if (Debug.isDebugBuild)
                        Debug.LogWarning($"没有找到事件 {eventType.FullName} 的处理程序");
                    return false;
                }
            }

            if (collection == null)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning($"事件 {eventType.Name} 的处理程序集合为 null");
                return false;
            }

            // 将处理程序信息同步到事件的监听器列表
            var handlers = collection.GetSortedHandlers();
            foreach (var handler in handlers)
            {
                eventArgs.GetListenerList().Add(handler.Handler, handler.Priority, handler.NumericPriority,
                    handler.ReceiveCanceled, handler.DebugInfo);
            }

            var wasHandled = false;

            foreach (var handlerInfo in handlers)
            {
                // 设置当前处理程序信息
                eventArgs.CurrentHandler = handlerInfo;

                // 设置事件阶段
                eventArgs.SetPhase(handlerInfo.Priority);

                // 检查是否应该跳过已取消的事件
                if (eventArgs is { IsCancelable: true, IsCanceled: true } && !handlerInfo.ReceiveCanceled)
                    continue;

                if (handlerInfo.Handler is Func<TEvent, Task> typedHandler)
                {
                    try
                    {
                        await typedHandler(eventArgs);
                        wasHandled = true;
                    }
                    catch (Exception ex)
                    {
                        if (Debug.isDebugBuild)
                            Debug.LogError($"执行事件 {eventType.Name} 处理程序时发生异常: {ex}");
                    }
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError($"事件 {eventType.Name} 的处理程序类型不匹配。" +
                                       $"预期 Func<{eventType.Name}, Task>，实际为 {handlerInfo.Handler.GetType().Name}");
                }
            }

            eventArgs.CurrentHandler = null;

            return wasHandled;
        }

        /// <summary>
        /// 同步触发指定类型的事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TriggerEvent<TEvent>(TEvent eventArgs) where TEvent : EventBase
        {
            return TriggerEventAsync(eventArgs).GetAwaiter().GetResult();
        }

        #endregion

        #region 查询和管理 API

        /// <summary>
        /// 获取指定事件类型的所有订阅者信息
        /// </summary>
        public static EventHandlerInfo[] GetEventSubscribers<TEvent>() where TEvent : EventBase
        {
            var eventType = typeof(TEvent);
            lock (EventHandlers)
            {
                return EventHandlers.TryGetValue(eventType, out var collection)
                    ? collection.GetAllHandlers()
                    : Array.Empty<EventHandlerInfo>();
            }
        }

        /// <summary>
        /// 获取指定事件类型的监听器列表
        /// </summary>
        public static ListenerList GetListenerList<TEvent>() where TEvent : EventBase
        {
            var eventType = typeof(TEvent);
            lock (EventHandlers)
            {
                if (EventHandlers.TryGetValue(eventType, out var collection))
                    return collection;
                return new ListenerList();
            }
        }

        /// <summary>
        /// 自动注册对象的事件处理程序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AutoRegister(object target)
        {
            if (target == null)
            {
                if (Debug.isDebugBuild)
                    Debug.LogError("AutoRegister: 目标对象为 null。");
                return;
            }

            lock (InstanceLock)
            {
                if (RegisteredInstances.Contains(target))
                    return;

                EventBusRegHelper.RegisterEventHandlers(target);
                RegisteredInstances.Add(target);
            }
        }

        /// <summary>
        /// 检查指定实例是否已注册
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInstanceRegistered(object target)
        {
            if (target == null) return false;

            lock (InstanceLock)
            {
                return RegisteredInstances.Contains(target);
            }
        }

        /// <summary>
        /// 获取当前已注册的实例数量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRegisteredInstanceCount()
        {
            lock (InstanceLock)
            {
                return RegisteredInstances.Count;
            }
        }

        /// <summary>
        /// 获取当前已注册的事件类型数量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRegisteredEventTypeCount()
        {
            lock (EventHandlers)
            {
                return EventHandlers.Count;
            }
        }

        /// <summary>
        /// 获取指定事件类型的详细调试信息
        /// </summary>
        public static string GetEventTypeDebugInfo<TEvent>() where TEvent : EventBase
        {
            var eventType = typeof(TEvent);

            lock (EventHandlers)
            {
                if (!EventHandlers.TryGetValue(eventType, out var collection))
                    return $"事件类型 {eventType.FullName} 未注册任何处理程序";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"事件类型: {eventType.FullName}");
                sb.AppendLine($"处理程序数量: {collection.Count}");
                sb.AppendLine("处理程序详情 (按优先级排序):");
                sb.Append(collection.GetDebugInfo());
                return sb.ToString();
            }
        }

        /// <summary>
        /// 获取所有已注册事件类型的统计信息
        /// </summary>
        public static Dictionary<Type, int> GetEventStatistics()
        {
            lock (EventHandlers)
            {
                return EventHandlers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
            }
        }

        #endregion
    }
}