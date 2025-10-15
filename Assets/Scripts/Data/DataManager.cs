using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ShrinkEventBus;
using UnityEngine;

// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace Data
{
    /// <summary>
    /// 玩家数据加载完成事件
    /// </summary>
    public class PlayerDataLoadedEvent : EventBase
    {
        public string SlotName { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// 玩家数据保存完成事件
    /// </summary>
    public class PlayerDataSavedEvent : EventBase
    {
        public string SlotName { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// 玩家数据更新事件
    /// </summary>
    public class PlayerDataUpdatedEvent : EventBase
    {
        public string SlotName { get; set; }
        public string Key { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// 玩家数据重置事件
    /// </summary>
    public class PlayerDataResetEvent : EventBase
    {
        public string SlotName { get; set; }
    }

    /// <summary>
    /// 玩家数据错误事件
    /// </summary>
    public class PlayerDataErrorEvent : EventBase
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// 存档槽位创建事件
    /// </summary>
    public class PlayerSlotCreatedEvent : EventBase
    {
        public string SlotName { get; set; }
    }

    /// <summary>
    /// 存档槽位删除事件
    /// </summary>
    public class PlayerSlotDeletedEvent : EventBase
    {
        public string SlotName { get; set; }
    }

    /// <summary>
    /// 存档槽位切换事件
    /// </summary>
    public class PlayerSlotChangedEvent : EventBase
    {
        public string OldSlot { get; set; }
        public string NewSlot { get; set; }
    }

    /// <summary>
    /// 游戏数据管理器
    /// <para>负责玩家数据的加载、保存、加密和管理</para>
    /// <para>支持多存档槽位和自动保存功能</para>
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        #region 单例

        private static DataManager _instance;

        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("DataManager");
                    _instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #endregion
        

        #region 配置参数

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        private const int AutoSaveInterval = 300;

        /// <summary>
        /// 默认存档槽位名称
        /// </summary>
        private const string DefaultSlot = "Default";

        /// <summary>
        /// 存档文件夹名称
        /// </summary>
        private const string SaveFolder = "Saves";

        #endregion

        #region 核心状态

        /// <summary>
        /// 当前加载的数据
        /// </summary>
        private Dictionary<string, object> _currentData = new();

        /// <summary>
        /// 自动保存计时器
        /// </summary>
        private float _saveTimer;

        /// <summary>
        /// 当前使用的存档槽位
        /// </summary>
        private string _currentSlot = DefaultSlot;

        /// <summary>
        /// 标记是否已经初始化加载过数据
        /// </summary>
        private bool _hasLoadedInitialData = false;

        /// <summary>
        /// 是否启用数据加密
        /// </summary>
        private bool _encryptionEnabled = false;

        #endregion

        #region 公开属性

        /// <summary>
        /// 当前存档槽位
        /// <para>设置此属性会自动切换到指定槽位</para>
        /// </summary>
        public string CurrentSlot
        {
            get => _currentSlot;
            set => SwitchSlot(value);
        }

        /// <summary>
        /// 是否启用数据加密
        /// </summary>
        public bool EncryptionEnabled
        {
            get => _encryptionEnabled;
            set => _encryptionEnabled = value;
        }

        #endregion

        #region 生命周期

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDataManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化数据管理器
        /// <para>确保存档目录存在，加载默认存档，设置自动保存</para>
        /// </summary>
        private void InitializeDataManager()
        {
            EnsureSaveDirectory();
            SwitchSlot(DefaultSlot);
            SetupAutoSave();
        }

        /// <summary>
        /// 每帧处理
        /// <para>管理自动保存计时器</para>
        /// </summary>
        private void Update()
        {
            if (_saveTimer > 0 && (_saveTimer -= Time.deltaTime) <= 0)
            {
                ForceSave();
                SetupAutoSave();
            }
        }

        /// <summary>
        /// 应用程序退出时自动保存
        /// </summary>
        private void OnApplicationQuit()
        {
            ForceSave();
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 获取指定键的数据
        /// <para>如果数据不存在，则使用默认值并保存</para>
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>获取的数据值或默认值</returns>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (TryGetValue(key, out T value)) return value;
            SetData(key, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// 设置指定键的数据
        /// <para>触发事件：PlayerDataUpdatedEvent</para>
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="newValue">新值</param>
        /// <param name="immediateSave">是否立即保存</param>
        /// <returns>是否成功设置（值发生变化）</returns>
        public bool SetData<T>(string key, T newValue, bool immediateSave = false)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var oldValue = _currentData.ContainsKey(key) ? _currentData[key] : null;
            if (Equals(oldValue, newValue)) return false;

            _currentData[key] = newValue;
            TriggerUpdateEvent(key, oldValue, newValue);

            if (immediateSave) ForceSave();
            else ResetSaveTimer();

            return true;
        }

        /// <summary>
        /// 重置当前槽位数据
        /// <para>触发事件：PlayerDataResetEvent</para>
        /// </summary>
        public void ResetData()
        {
            _currentData.Clear();
            ForceSave();

            // 触发数据重置事件
            EventBus.TriggerEvent(new PlayerDataResetEvent
            {
                SlotName = _currentSlot
            });
        }

        /// <summary>
        /// 创建新的存档槽位
        /// <para>触发事件：PlayerSlotCreatedEvent</para>
        /// </summary>
        /// <param name="slotName">槽位名称</param>
        public void CreateNewSlot(string slotName)
        {
            if (SlotExists(slotName)) return;

            var path = GetSlotPath(slotName);
            try
            {
                File.WriteAllText(path, EncryptData("{}"));

                // 触发槽位创建事件
                EventBus.TriggerEvent(new PlayerSlotCreatedEvent
                {
                    SlotName = slotName
                });
            }
            catch (Exception e)
            {
                HandleError($"创建槽位失败: {e.Message}", e);
            }
        }

        /// <summary>
        /// 删除存档槽位
        /// <para>触发事件：PlayerSlotDeletedEvent</para>
        /// </summary>
        /// <param name="slotName">槽位名称</param>
        public void DeleteSlot(string slotName)
        {
            var path = GetSlotPath(slotName);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);

                    // 触发槽位删除事件
                    EventBus.TriggerEvent(new PlayerSlotDeletedEvent
                    {
                        SlotName = slotName
                    });
                }
                catch (Exception e)
                {
                    HandleError($"删除槽位失败: {e.Message}", e);
                }
            }
        }

        /// <summary>
        /// 获取所有存档槽位名称
        /// </summary>
        /// <returns>槽位名称列表</returns>
        public List<string> GetAllSlots()
        {
            var slots = new List<string>();
            var saveDir = GetSaveDirectory();

            if (!Directory.Exists(saveDir))
                return slots;

            try
            {
                var files = Directory.GetFiles(saveDir, "*.sav");
                foreach (var file in files)
                {
                    slots.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception e)
            {
                HandleError($"获取槽位列表失败: {e.Message}", e);
            }

            return slots;
        }

        /// <summary>
        /// 切换到指定存档槽位
        /// <para>触发事件：PlayerSlotChangedEvent, PlayerDataLoadedEvent</para>
        /// </summary>
        /// <param name="slotName">槽位名称</param>
        public void SwitchSlot(string slotName)
        {
            // 首次加载时即使槽位名相同也要执行
            if (_hasLoadedInitialData && _currentSlot == slotName) return;

            var oldSlot = _currentSlot;
            
            // 如果不是首次加载，先保存当前槽位
            if (_hasLoadedInitialData)
            {
                ForceSave();
            }
            
            _currentSlot = slotName;
            LoadCurrentSlot();
            _hasLoadedInitialData = true;

            // 触发槽位切换事件（首次加载时不触发切换事件）
            if (oldSlot != slotName)
            {
                EventBus.TriggerEvent(new PlayerSlotChangedEvent
                {
                    OldSlot = oldSlot,
                    NewSlot = slotName
                });
            }
        }

        /// <summary>
        /// 强制立即保存当前存档槽数据
        /// <para>触发事件：PlayerDataSavedEvent</para>
        /// <para>关联错误：PlayerDataErrorEvent（保存失败时）</para>
        /// </summary>
        public void ForceSave()
        {
            try
            {
                var dataToSave = new Dictionary<string, object>(_currentData)
                {
                    ["LastSaveTime"] = DateTime.Now
                };

                var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                var finalData = _encryptionEnabled ? EncryptData(json) : json;

                File.WriteAllText(GetCurrentSlotPath(), finalData);

                // 触发保存完成事件
                EventBus.TriggerEvent(new PlayerDataSavedEvent
                {
                    SlotName = _currentSlot,
                    Data = dataToSave
                });
            }
            catch (Exception e)
            {
                HandleError($"数据保存失败: {e.Message}", e);
            }
        }

        #endregion

        #region 加密功能

        /// <summary>
        /// 加密数据
        /// <para>使用AES加密算法将JSON字符串加密为Base64字符串</para>
        /// </summary>
        /// <param name="plainText">待加密的明文</param>
        /// <returns>加密后的Base64字符串</returns>
        private string EncryptData(string plainText)
        {
            if (!_encryptionEnabled) return plainText;

            try
            {
                using var aes = Aes.Create();
                var keyBytes = new byte[32];
                var ivBytes = new byte[16];

                const string keyString = "YourGameSecretKey2024";
                const string ivString = "YourGameIV2024";

                using (var sha256 = SHA256.Create())
                {
                    var keyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
                    Array.Copy(keyHash, keyBytes, 32);
                }

                using (var md5 = MD5.Create())
                {
                    var ivHash = md5.ComputeHash(Encoding.UTF8.GetBytes(ivString));
                    Array.Copy(ivHash, ivBytes, 16);
                }

                aes.Key = keyBytes;
                aes.IV = ivBytes;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                HandleError($"数据加密失败: {ex.Message}", ex);
                return plainText;
            }
        }

        /// <summary>
        /// 解密数据
        /// <para>使用AES解密算法将Base64字符串解密为JSON字符串</para>
        /// </summary>
        /// <param name="cipherText">待解密的Base64字符串</param>
        /// <returns>解密后的JSON字符串</returns>
        private string DecryptData(string cipherText)
        {
            if (!_encryptionEnabled) return cipherText;

            if (string.IsNullOrEmpty(cipherText) || !IsValidBase64(cipherText))
            {
                return cipherText;
            }

            try
            {
                using var aes = Aes.Create();

                var keyBytes = new byte[32];
                var ivBytes = new byte[16];

                const string keyString = "YourGameSecretKey2024";
                const string ivString = "YourGameIV2024";

                using (var sha256 = SHA256.Create())
                {
                    var keyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
                    Array.Copy(keyHash, keyBytes, 32);
                }

                using (var md5 = MD5.Create())
                {
                    var ivHash = md5.ComputeHash(Encoding.UTF8.GetBytes(ivString));
                    Array.Copy(ivHash, ivBytes, 16);
                }

                aes.Key = keyBytes;
                aes.IV = ivBytes;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                HandleError($"数据解密失败: {ex.Message}", ex);
                return cipherText;
            }
        }

        /// <summary>
        /// 检查字符串是否为有效的Base64格式
        /// </summary>
        /// <param name="base64String">待检查的字符串</param>
        /// <returns>是否为有效的Base64格式</returns>
        private bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return false;

            try
            {
                if (base64String.Length % 4 != 0) return false;

                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载当前槽位的数据
        /// <para>触发事件：PlayerDataLoadedEvent, PlayerDataErrorEvent（加载失败时）</para>
        /// </summary>
        private void LoadCurrentSlot()
        {
            try
            {
                var path = GetCurrentSlotPath();
                if (!File.Exists(path))
                {
                    _currentData = new Dictionary<string, object>();
                }
                else
                {
                    var content = File.ReadAllText(path);
                    var json = _encryptionEnabled ? DecryptData(content) : content;
                    _currentData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                                   ?? new Dictionary<string, object>();
                }

                // 无论文件是否存在，都触发数据加载完成事件
                EventBus.TriggerEvent(new PlayerDataLoadedEvent
                {
                    SlotName = _currentSlot,
                    Data = new Dictionary<string, object>(_currentData)
                });
            }
            catch (Exception e)
            {
                HandleError($"数据加载失败: {e.Message}", e);
                _currentData = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 尝试获取指定键的数据值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="value">输出的数据值</param>
        /// <returns>是否成功获取</returns>
        private bool TryGetValue<T>(string key, out T value)
        {
            if (_currentData.TryGetValue(key, out var rawValue))
            {
                try
                {
                    // 处理 Newtonsoft.Json 的 JArray/JObject 等类型
                    if (rawValue is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        value = jToken.ToObject<T>();
                        return value != null;
                    }
                    
                    // 尝试直接转换
                    if (rawValue is T directValue)
                    {
                        value = directValue;
                        return true;
                    }
                    
                    // 使用 Convert.ChangeType 进行转换
                    value = (T)Convert.ChangeType(rawValue, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 触发数据更新事件
        /// <para>触发事件：PlayerDataUpdatedEvent</para>
        /// </summary>
        /// <param name="key">更新的数据键</param>
        /// <param name="oldValue">更新前的值</param>
        /// <param name="newValue">更新后的值</param>
        private void TriggerUpdateEvent(string key, object oldValue, object newValue)
        {
            EventBus.TriggerEvent(new PlayerDataUpdatedEvent
            {
                SlotName = _currentSlot,
                Key = key,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        /// <summary>
        /// 获取存档目录路径
        /// </summary>
        /// <returns>存档目录的完整路径</returns>
        private string GetSaveDirectory()
        {
            return Path.Combine(
                Application.persistentDataPath,
                SaveFolder
            );
        }

        /// <summary>
        /// 获取当前槽位的存档文件路径
        /// </summary>
        /// <returns>当前槽位存档文件的完整路径</returns>
        private string GetCurrentSlotPath()
        {
            return GetSlotPath(_currentSlot);
        }

        /// <summary>
        /// 获取指定槽位的存档文件路径
        /// </summary>
        /// <param name="slotName">槽位名称</param>
        /// <returns>指定槽位存档文件的完整路径</returns>
        private string GetSlotPath(string slotName)
        {
            return Path.Combine(
                GetSaveDirectory(),
                $"{slotName}.sav"
            );
        }

        /// <summary>
        /// 检查指定槽位是否存在
        /// </summary>
        /// <param name="slotName">槽位名称</param>
        /// <returns>是否存在</returns>
        private bool SlotExists(string slotName)
        {
            return File.Exists(GetSlotPath(slotName));
        }

        /// <summary>
        /// 确保存档目录存在
        /// </summary>
        private void EnsureSaveDirectory()
        {
            var path = GetSaveDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 处理错误
        /// <para>触发事件：PlayerDataErrorEvent</para>
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="exception">异常对象（可选）</param>
        private void HandleError(string message, Exception exception = null)
        {

            // 触发错误事件
            EventBus.TriggerEvent(new PlayerDataErrorEvent
            {
                ErrorMessage = message,
                Exception = exception
            });
        }

        /// <summary>
        /// 设置自动保存计时器
        /// </summary>
        private void SetupAutoSave()
        {
            _saveTimer = AutoSaveInterval;
        }

        /// <summary>
        /// 重置自动保存计时器
        /// </summary>
        private void ResetSaveTimer()
        {
            _saveTimer = AutoSaveInterval;
        }

        #endregion
    }
}