using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ShrinkEventBus
{
    public static class EventAutoRegHelper
    {
        /// <summary>
        /// 存储发现的订阅者类型
        /// </summary>
        private static readonly ConcurrentDictionary<Type, bool> SubscriberTypes = new();

        /// <summary>
        /// 存储已注册的节点实例
        /// </summary>
        private static readonly ConcurrentDictionary<object, bool> RegisteredNodes = new();

        /// <summary>
        /// 存储已处理的节点ID
        /// </summary>
        private static readonly ConcurrentDictionary<int, bool> ProcessedNodeIds = new();

        /// <summary>
        /// 是否正在监控对象变化
        /// </summary>
        private static bool _isMonitoring;

        /// <summary>
        /// 初始化锁
        /// </summary>
        private static readonly object InitLock = new();

        /// <summary>
        /// 静态构造函数，启动异步初始化
        /// </summary>
        static EventAutoRegHelper()
        {
            _ = Task.Run(InitializeAsync);
        }

        /// <summary>
        /// 异步初始化方法
        /// </summary>
        private static async Task InitializeAsync()
        {
            // Unity 场景在主线程加载，等待首帧
            await Task.Delay(100);

            // 在主线程初始化
            UnityMainThreadDispatcher.Enqueue(() => InitializeSystem());
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        private static void InitializeSystem()
        {
            lock (InitLock)
            {
                if (IsInitialized) return;

                // 扫描所有 EventBus 订阅者类型
                ScanEventBusSubscribers();

                // 启动监控
                StartMonitoring();

                // 扫描现有 MonoBehaviour 对象
                ScanExistingObjects();

                IsInitialized = true;

                if (Debug.isDebugBuild)
                    Debug.Log($"EventAutoRegHelper 初始化完成，发现 {SubscriberTypes.Count} 个订阅者类型");
            }
        }

        /// <summary>
        /// 扫描现有场景中的所有对象
        /// </summary>
        private static void ScanExistingObjects()
        {
            // 查找所有 MonoBehaviour（包括非激活的）
            var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

            if (Debug.isDebugBuild)
                Debug.Log($"扫描到 {allMonoBehaviours.Length} 个 MonoBehaviour 对象");

            foreach (var mb in allMonoBehaviours)
            {
                ProcessNodeRegistration(mb);
            }
        }

        /// <summary>
        /// 确保自动注册管理器已初始化
        /// </summary>
        public static void EnsureInitialized()
        {
            if (IsInitialized) return;

            lock (InitLock)
            {
                if (!IsInitialized)
                    InitializeSystem();
            }
        }

        /// <summary>
        /// 扫描程序集发现订阅者类型
        /// </summary>
        private static void ScanEventBusSubscribers()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var foundTypes = 0;

            foreach (var assembly in assemblies)
            {
                try
                {
                    if (IsSystemAssembly(assembly)) continue;

                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.GetCustomAttribute<EventBusSubscriberAttribute>() == null ||
                            !typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                        if (!SubscriberTypes.TryAdd(type, true)) continue;
                        foundTypes++;
                        if (Debug.isDebugBuild)
                            Debug.Log($"发现订阅者类型: {type.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    if (Debug.isDebugBuild)
                        Debug.LogError($"扫描程序集失败: {assembly.GetName().Name} - {ex.Message}");
                }
            }

            if (Debug.isDebugBuild)
                Debug.Log($"订阅者类型扫描完成，共发现 {foundTypes} 个类型");
        }

        /// <summary>
        /// 检查是否为系统程序集
        /// </summary>
        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.FullName ?? "";
            return name.StartsWith("System.") ||
                   name.StartsWith("Microsoft.") ||
                   name.StartsWith("mscorlib") ||
                   name.StartsWith("netstandard") ||
                   name.StartsWith("Unity.") ||
                   name.StartsWith("UnityEngine.") ||
                   name.StartsWith("UnityEditor.");
        }

        /// <summary>
        /// 启动对象生命周期监控
        /// </summary>
        private static void StartMonitoring()
        {
            if (_isMonitoring) return;

            // Unity 使用回调监控对象
            UnityEventCallbacks.OnObjectCreated += OnObjectCreated;
            UnityEventCallbacks.OnObjectDestroyed += OnObjectDestroyed;

            _isMonitoring = true;

            if (Debug.isDebugBuild)
                Debug.Log("EventAutoRegHelper 监控已启动");
        }

        /// <summary>
        /// 处理对象创建事件
        /// </summary>
        private static void OnObjectCreated(MonoBehaviour obj)
        {
            if (obj == null) return;
            ProcessNodeRegistration(obj);
        }

        /// <summary>
        /// 处理对象销毁事件
        /// </summary>
        private static void OnObjectDestroyed(MonoBehaviour obj)
        {
            if (obj == null) return;
            UnregisterNode(obj);
        }

        /// <summary>
        /// 处理节点注册逻辑
        /// </summary>
        private static void ProcessNodeRegistration(object node)
        {
            try
            {
                if (node == null) return;

                var nodeType = node.GetType();
                var instanceId = node.GetHashCode();

                // 检查是否是订阅者类型
                if (!SubscriberTypes.ContainsKey(nodeType))
                    return;

                // 避免重复处理
                if (ProcessedNodeIds.ContainsKey(instanceId))
                    return;

                ProcessedNodeIds.TryAdd(instanceId, true);
                RegisterNode(node);
            }
            catch (Exception ex)
            {
                if (Debug.isDebugBuild)
                    Debug.LogError($"处理节点注册时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册单个节点
        /// </summary>
        private static void RegisterNode(object node)
        {
            if (node == null) return;

            lock (RegisteredNodes)
            {
                if (RegisteredNodes.ContainsKey(node))
                    return;

                try
                {
                    RegisteredNodes.TryAdd(node, true);
                    EventBus.AutoRegister(node);

                    if (Debug.isDebugBuild)
                        Debug.Log($"自动注册成功: {node.GetType().Name}");
                }
                catch (Exception ex)
                {
                    RegisteredNodes.TryRemove(node, out _);
                    Debug.LogError($"自动注册失败 {node.GetType().FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public static (int SubscriberTypes, int RegisteredNodes, int ProcessedNodes, int EventBusInstances, int
            EventTypes) GetStatistics()
        {
            return (
                SubscriberTypes.Count,
                RegisteredNodes.Count,
                ProcessedNodeIds.Count,
                EventBus.GetRegisteredInstanceCount(),
                EventBus.GetRegisteredEventTypeCount()
            );
        }

        /// <summary>
        /// 注销节点
        /// </summary>
        public static void UnregisterNode(object node)
        {
            if (node == null) return;

            lock (RegisteredNodes)
            {
                RegisteredNodes.TryRemove(node, out _);
                ProcessedNodeIds.TryRemove(node.GetHashCode(), out _);
            }

            EventBus.UnregisterInstance(node);

            if (Debug.isDebugBuild)
                Debug.Log($"节点已注销: {node.GetType().Name}");
        }

        /// <summary>
        /// 强制扫描当前场景
        /// </summary>
        public static void ForceScanCurrentScene()
        {
            Debug.Log("开始强制扫描当前场景");
            ScanExistingObjects();
            Debug.Log("强制扫描完成");
        }

        /// <summary>
        /// 获取是否已初始化
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        public static void Cleanup()
        {
            lock (InitLock)
            {
                if (_isMonitoring)
                {
                    UnityEventCallbacks.OnObjectCreated -= OnObjectCreated;
                    UnityEventCallbacks.OnObjectDestroyed -= OnObjectDestroyed;
                }

                RegisteredNodes.Clear();
                ProcessedNodeIds.Clear();
                SubscriberTypes.Clear();

                IsInitialized = false;
                _isMonitoring = false;

                Debug.Log("自动注册管理器已清理");
            }
        }

        /// <summary>
        /// 获取详细统计信息
        /// </summary>
        public static string GetDetailedStatistics()
        {
            var stats = GetStatistics();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== EventAutoRegHelper 统计信息 ===");
            sb.AppendLine($"订阅者类型数: {stats.SubscriberTypes}");
            sb.AppendLine($"已注册节点数: {stats.RegisteredNodes}");
            sb.AppendLine($"已处理节点数: {stats.ProcessedNodes}");
            sb.AppendLine($"EventBus实例数: {stats.EventBusInstances}");
            sb.AppendLine($"EventBus事件类型数: {stats.EventTypes}");
            sb.AppendLine($"是否已初始化: {IsInitialized}");
            sb.AppendLine($"是否正在监控: {_isMonitoring}");

            sb.AppendLine("\n已发现的订阅者类型:");
            foreach (var type in SubscriberTypes.Keys)
            {
                sb.AppendLine($"  - {type.FullName}");
            }

            return sb.ToString();
        }
    }
    
    public static class MonoBehaviourEventBusExtensions
    {
        public static void NotifyNodeRemoved(this MonoBehaviour node)
        {
            EventAutoRegHelper.UnregisterNode(node);
        }

        public static bool IsEventBusRegistered(this MonoBehaviour node)
        {
            return EventBus.IsInstanceRegistered(node);
        }

        public static void UnregisterFromEventBus(this MonoBehaviour node)
        {
            EventBus.UnregisterInstance(node);
            EventAutoRegHelper.UnregisterNode(node);
        }

        public static void RegisterToEventBus(this MonoBehaviour node)
        {
            EventBus.AutoRegister(node);
        }
    }
    
    public static class UnityEventCallbacks
    {
        public static event Action<MonoBehaviour> OnObjectCreated;
        public static event Action<MonoBehaviour> OnObjectDestroyed;

        internal static void NotifyObjectCreated(MonoBehaviour obj)
        {
            OnObjectCreated?.Invoke(obj);
        }

        internal static void NotifyObjectDestroyed(MonoBehaviour obj)
        {
            OnObjectDestroyed?.Invoke(obj);
        }
    }
    
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _executionQueue = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null) return;
            var go = new GameObject("EventBusDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            _executionQueue.Enqueue(action);
        }

        private void Update()
        {
            while (_executionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程执行错误: {ex}");
                }
            }
        }
    }
}