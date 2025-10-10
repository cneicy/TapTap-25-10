using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace ShrinkEventBus
{
    /// <summary>
    /// EventBus注册助手类 - 完整版本
    /// </summary>
    public static class EventBusRegHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegStaticEventHandler()
        {
            RegisterStaticEventHandlersWithReflection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEventHandlers(object target)
        {
            RegisterEventHandlersWithReflection(target);
        }

        /// <summary>
        /// 使用反射注册静态事件处理程序
        /// </summary>
        private static void RegisterStaticEventHandlersWithReflection()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        var classAttributes = type.GetCustomAttributes(typeof(EventBusSubscriberAttribute), false);
                        if (classAttributes.Length == 0) continue;

                        var methods =
                            type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                        foreach (var method in methods)
                        {
                            var attributes = method.GetCustomAttributes(typeof(EventSubscribeAttribute), false);
                            if (attributes.Length == 0) continue;

                            var subscribeAttr = (EventSubscribeAttribute)attributes[0];
                            var priority = subscribeAttr.Priority;
                            var numericPriority = subscribeAttr.NumericPriority;
                            var receiveCanceled = subscribeAttr.ReceiveCanceled;

                            var parameters = method.GetParameters();

                            if (parameters.Length != 1)
                            {
                                if (Debug.isDebugBuild)
                                    Debug.LogError(
                                        $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数数量不为1。");
                                continue;
                            }

                            var parameterType = parameters[0].ParameterType;
                            if (!typeof(EventBase).IsAssignableFrom(parameterType))
                            {
                                if (Debug.isDebugBuild)
                                    Debug.LogError(
                                        $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数类型 {parameterType.Name} 不继承自 EventBase。");
                                continue;
                            }

                            var isAsync = method.ReturnType == typeof(Task);

                            try
                            {
                                if (isAsync)
                                {
                                    var funcType = typeof(Func<,>).MakeGenericType(parameterType, typeof(Task));
                                    var handlerDelegate = Delegate.CreateDelegate(funcType, method);

                                    EventBus.RegisterEventInternal(parameterType, handlerDelegate,
                                        priority,
                                        numericPriority, receiveCanceled,
                                        $"Static {type.Name}.{method.Name} (Async, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                                        method);

                                    if (Debug.isDebugBuild)
                                        Debug.Log(
                                            $"自动注册静态事件: {parameterType.FullName} -> {type.Name}.{method.Name} (优先级: {priority}({numericPriority}))");
                                }
                                else if (method.ReturnType == typeof(void))
                                {
                                    var actionType = typeof(Action<>).MakeGenericType(parameterType);
                                    var actionDelegate = Delegate.CreateDelegate(actionType, method);

                                    var wrappedHandler =
                                        WrapSyncHandlerWithMethodInfo(parameterType, actionDelegate, method);
                                    EventBus.RegisterEventInternal(parameterType, wrappedHandler,
                                        priority, numericPriority,
                                        receiveCanceled,
                                        $"Static {type.Name}.{method.Name} (Sync, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                                        method);

                                    if (Debug.isDebugBuild)
                                        Debug.Log(
                                            $"自动注册静态事件: {parameterType.FullName} -> {type.Name}.{method.Name} (优先级: {priority}({numericPriority}))");
                                }
                                else
                                {
                                    if (Debug.isDebugBuild)
                                        Debug.LogError(
                                            $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但返回类型不是 void 或 Task。");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Debug.isDebugBuild)
                                    Debug.LogError($"为静态事件 {parameterType.FullName} 创建委托给方法 {method.Name} 失败: {ex}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError($"扫描程序集 {assembly.FullName} 时发生异常: {ex}");
                }
            }
        }

        /// <summary>
        /// 使用反射注册对象实例的事件处理程序
        /// </summary>
        private static void RegisterEventHandlersWithReflection(object target)
        {
            var type = target.GetType();

            var classAttributes = type.GetCustomAttributes(typeof(EventBusSubscriberAttribute), false);
            if (classAttributes.Length == 0) return;

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(EventSubscribeAttribute), false);
                if (attributes.Length == 0) continue;

                if (method.IsStatic) continue;

                var subscribeAttr = (EventSubscribeAttribute)attributes[0];
                var priority = subscribeAttr.Priority;
                var numericPriority = subscribeAttr.NumericPriority;
                var receiveCanceled = subscribeAttr.ReceiveCanceled;

                var parameters = method.GetParameters();

                if (parameters.Length != 1)
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError($"方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数数量不为1。");
                    continue;
                }

                var parameterType = parameters[0].ParameterType;
                if (!typeof(EventBase).IsAssignableFrom(parameterType))
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError(
                            $"方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数类型 {parameterType.Name} 不继承自 EventBase。");
                    continue;
                }

                var isAsync = method.ReturnType == typeof(Task);

                try
                {
                    if (isAsync)
                    {
                        var funcType = typeof(Func<,>).MakeGenericType(parameterType, typeof(Task));
                        var handlerDelegate = Delegate.CreateDelegate(funcType, target, method);

                        EventBus.RegisterEventInternal(parameterType, handlerDelegate, priority,
                            numericPriority,
                            receiveCanceled,
                            $"Instance {type.Name}.{method.Name} (Async, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                            method);
                    }
                    else if (method.ReturnType == typeof(void))
                    {
                        var actionType = typeof(Action<>).MakeGenericType(parameterType);
                        var actionDelegate = Delegate.CreateDelegate(actionType, target, method);

                        var wrappedHandler = WrapSyncHandlerWithMethodInfo(parameterType, actionDelegate, method);
                        EventBus.RegisterEventInternal(parameterType, wrappedHandler, priority,
                            numericPriority,
                            receiveCanceled,
                            $"Instance {type.Name}.{method.Name} (Sync, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                            method);
                    }
                }
                catch (Exception ex)
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError($"为事件 {parameterType.FullName} 创建委托给方法 {method.Name} 失败: {ex}");
                }
            }
        }

        /// <summary>
        /// 将同步处理程序包装为异步版本（保留原始方法信息）
        /// </summary>
        private static Delegate WrapSyncHandlerWithMethodInfo(Type eventType, Delegate syncHandler,
            MethodInfo originalMethod)
        {
            var method = typeof(EventBusRegHelper)
                .GetMethod(nameof(WrapActionWithMethodInfo), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(eventType);

            if (method == null)
                throw new InvalidOperationException("无法找到WrapActionWithMethodInfo方法");

            return (Delegate)method.Invoke(null, new object[] { syncHandler, originalMethod })!;
        }

        /// <summary>
        /// 创建一个保留原始方法信息的包装器
        /// </summary>
        private static Func<T, Task> WrapActionWithMethodInfo<T>(Delegate actionDelegate, MethodInfo originalMethod)
        {
            var typedAction = (Action<T>)actionDelegate;
            var wrapper = new MethodInfoPreservingWrapper<T>(typedAction, originalMethod);
            return wrapper.ExecuteAsync;
        }

        /// <summary>
        /// 保留方法信息的包装器类
        /// </summary>
        private class MethodInfoPreservingWrapper<T>
        {
            private readonly Action<T> _originalAction;
            private readonly MethodInfo _originalMethod;

            public MethodInfoPreservingWrapper(Action<T> originalAction, MethodInfo originalMethod)
            {
                _originalAction = originalAction;
                _originalMethod = originalMethod;
            }

            public MethodInfo OriginalMethod => _originalMethod;

            public Task ExecuteAsync(T arg)
            {
                try
                {
                    _originalAction(arg);
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
        }

        /// <summary>
        /// 将同步处理程序包装为异步版本（不保留方法信息）
        /// </summary>
        private static Delegate WrapSyncHandler(Type eventType, Delegate syncHandler)
        {
            var method = typeof(EventBusRegHelper)
                .GetMethod(nameof(WrapAction), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(eventType);

            if (method == null)
                throw new InvalidOperationException("无法找到WrapAction方法");

            return (Delegate)method.Invoke(null, new object[] { syncHandler })!;
        }

        /// <summary>
        /// 将强类型的同步Action包装为异步Func
        /// </summary>
        private static Func<T, Task> WrapAction<T>(Delegate actionDelegate)
        {
            var typedAction = (Action<T>)actionDelegate;
            return arg =>
            {
                try
                {
                    typedAction(arg);
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            };
        }
    }
}