using System;
using Data;
using JetBrains.Annotations;
using ShrinkEventBus;
using TMPro;
using UnityEngine;
using Utils;
using Whisper;
using Whisper.Utils;

namespace Game.VoiceToText
{
    public class DirtyTalkEvent : EventBase{}
    public class EggTalkEvent : EventBase{}
    
    /// <summary>
    /// 实时流式语音识别管理器
    /// 麦克风常开，边录边识别
    /// 
    /// 使用说明：
    /// 1. 在场景中创建GameObject并添加 WhisperManager 组件
    /// 2. 在 WhisperManager 的 Inspector 中配置模型
    /// 3. 添加本组件并关联 WhisperManager
    /// </summary>
    public class VoiceStreamManager : Singleton<VoiceStreamManager>
    {
        [Header("组件引用")] [Tooltip("拖拽场景中的 WhisperManager 组件")]
        public WhisperManager whisperManager;

        [Header("录音配置")] [Tooltip("采样率(Hz)")] public int sampleRate = 16000;

        [Tooltip("麦克风设备索引，-1为默认设备")] public int microphoneDeviceIndex = -1;

        [Header("流式识别配置")] [Tooltip("每次处理的音频长度(秒)")] [Range(1f, 10f)]
        public float stepSec = 3f;

        [Tooltip("保留上次的音频长度(秒)，用于上下文")] [Range(0.1f, 2f)]
        public float keepSec = 0.2f;

        [Tooltip("完整识别的音频窗口长度(秒)")] [Range(5f, 30f)]
        public float lengthSec = 10f;

        [Tooltip("是否自动更新识别提示词")] public bool updatePrompt = true;

        [Tooltip("是否丢弃旧的缓冲区")] public bool dropOldBuffer = true;

        [Tooltip("是否使用语音活动检测(VAD)")] public bool useVad = true;

        [Header("显示设置")] [Tooltip("是否在控制台实时输出识别结果")]
        public bool printRealtimeResults = true;

        [Header("孩子不懂事写着玩的 可选")] [CanBeNull] public TMP_Text status;

        [Tooltip("是否显示调试信息")] public bool showDebugInfo = true;

        public event Action<string> OnSegmentReceived;
        public event Action<string> OnFinalResult;
        public event Action<string> OnError;
        public event Action OnStreamStarted;
        public event Action OnStreamStopped;

        private WhisperStream _stream;
        private MicrophoneRecord _microphoneRecord;
        private bool _isStreaming;
        private string _accumulatedText = "";
        private bool _eggTriggered;

        protected override void Awake()
        {
            base.Awake();
            if (!whisperManager)
            {
                Debug.LogError("❌ 未设置 WhisperManager 引用！");
                return;
            }

            InitializeMicrophone();
        }

        private void Start()
        {
            UpdateWhisperStreamSettings();

            if (_microphoneRecord)
            {
                SetMicrophone(microphoneDeviceIndex);
            }
        }

        /// <summary>
        /// 初始化麦克风
        /// </summary>
        private void InitializeMicrophone()
        {
            _microphoneRecord = gameObject.AddComponent<MicrophoneRecord>();
            _microphoneRecord.frequency = sampleRate;
            _microphoneRecord.maxLengthSec = 60;
            _microphoneRecord.loop = true; // 循环录音

            // 设置麦克风的chunk参数，确保能触发OnChunkReady事件
            _microphoneRecord.chunksLengthSec = 0.1f; // 每0.1秒一个chunk

            // 启用VAD
            _microphoneRecord.useVad = true;
            _microphoneRecord.vadUpdateRateSec = 0.1f;

            // 禁用自动停止
            _microphoneRecord.vadStop = false;

            // 禁用麦克风的echo（我们自己处理）
            _microphoneRecord.echo = false;
            StartStream();
            if (showDebugInfo)
            {
                Debug.Log($"麦克风初始化完成 - 频率:{sampleRate}Hz, Chunk:{_microphoneRecord.chunksLengthSec}s");
            }
        }

        /// <summary>
        /// 更新流式识别配置
        /// </summary>
        private void UpdateWhisperStreamSettings()
        {
            if (!whisperManager) return;

            whisperManager.stepSec = stepSec;
            whisperManager.keepSec = keepSec;
            whisperManager.lengthSec = lengthSec;
            whisperManager.updatePrompt = updatePrompt;
            whisperManager.dropOldBuffer = dropOldBuffer;
            whisperManager.useVad = useVad;
        }

        /// <summary>
        /// 获取所有麦克风设备
        /// </summary>
        public string[] GetAllMicrophones()
        {
            return Microphone.devices;
        }

        /// <summary>
        /// 设置麦克风设备
        /// </summary>
        public void SetMicrophone(int index)
        {
            if (!_microphoneRecord)
            {
                Debug.LogWarning("麦克风组件未初始化");
                return;
            }

            if (_isStreaming)
            {
                Debug.LogWarning("请先停止流式识别再切换麦克风");
                return;
            }

            if (index >= 0 && index < Microphone.devices.Length)
            {
                microphoneDeviceIndex = index;
                _microphoneRecord.SelectedMicDevice = Microphone.devices[index];
                if (showDebugInfo)
                {
                    Debug.Log($"切换麦克风: {Microphone.devices[index]}");
                }
            }
            else
            {
                microphoneDeviceIndex = -1;
                _microphoneRecord.SelectedMicDevice = null;
                if (showDebugInfo)
                {
                    Debug.Log("切换到默认麦克风");
                }
            }
        }

        /// <summary>
        /// 开始流式识别
        /// </summary>
        public async void StartStream()
        {
            if (_isStreaming)
            {
                Debug.LogWarning("流式识别已在运行");
                return;
            }

            if (!whisperManager)
            {
                Debug.LogError("WhisperManager 引用为空");
                OnError?.Invoke("WhisperManager 引用为空");
                return;
            }

            if (!_microphoneRecord)
            {
                Debug.LogError("麦克风组件未初始化");
                OnError?.Invoke("麦克风组件未初始化");
                return;
            }

            try
            {
                Debug.Log("启动流式语音识别...");

                if (!whisperManager.IsLoaded)
                {
                    Debug.Log("正在加载模型...");
                    await whisperManager.InitModel();

                    while (whisperManager.IsLoading)
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }
                }

                if (!whisperManager.IsLoaded)
                {
                    OnError?.Invoke("模型加载失败");
                    if (status) status.text = "faild";
                    return;
                }

                // 更新配置
                UpdateWhisperStreamSettings();

                if (showDebugInfo)
                {
                    Debug.Log($"流式参数 - Step:{stepSec}s, Keep:{keepSec}s, Length:{lengthSec}s");
                }

                // 先启动麦克风
                if (showDebugInfo)
                {
                    Debug.Log("启动麦克风录音...");
                }

                try
                {
                    _microphoneRecord.StartRecord();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"启动麦克风失败: {ex.Message}");
                    OnError?.Invoke($"启动麦克风失败: {ex.Message}");
                    return;
                }

                // 等待一小段时间确保麦克风启动
                await System.Threading.Tasks.Task.Delay(200);

                if (!_microphoneRecord.IsRecording)
                {
                    Debug.LogError("麦克风未能成功启动");
                    OnError?.Invoke("麦克风未能成功启动");
                    return;
                }

                // 创建流
                if (showDebugInfo)
                {
                    Debug.Log("创建 WhisperStream...");
                }

                _stream = await whisperManager.CreateStream(_microphoneRecord);

                if (_stream == null)
                {
                    OnError?.Invoke("创建流失败");
                    _microphoneRecord.StopRecord();
                    return;
                }

                // 注册流事件（正确的签名）
                _stream.OnSegmentUpdated += OnStreamSegmentUpdated; // WhisperResult 参数
                _stream.OnSegmentFinished += OnStreamSegmentFinished; // WhisperResult 参数
                _stream.OnStreamFinished += OnStreamFinished; // string 参数

                // 启动流（必须在注册事件后调用）
                if (showDebugInfo)
                {
                    Debug.Log("启动 WhisperStream...");
                }

                if (!DataManager.Instance.GetData<bool>("MicrophoneEnabled") &&
                    DataManager.Instance.GetData<bool>("IsNotFirstStart"))
                {
                    if (_microphoneRecord != null && _microphoneRecord.IsRecording)
                    {
                        _microphoneRecord.StopRecord();
                    }

                    _isStreaming = false;
                    print("关闭流式");
                    return;
                }

                _stream.StartStream();

                _isStreaming = true;
                _accumulatedText = "";


                Debug.Log("✅ 流式识别已启动！麦克风录音中，开始说话...");
                if (showDebugInfo)
                {
                    Debug.Log($"麦克风设备: {_microphoneRecord.RecordStartMicDevice ?? "默认"}");
                    Debug.Log($"是否正在录音: {_microphoneRecord.IsRecording}");
                }

                OnStreamStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"启动流式识别失败: {ex.Message}");
                Debug.LogException(ex);
                OnError?.Invoke($"启动失败: {ex.Message}");

                // 清理
                if (_microphoneRecord && _microphoneRecord.IsRecording)
                {
                    _microphoneRecord.StopRecord();
                }
            }
        }

        /// <summary>
        /// 停止流式识别
        /// </summary>
        public void StopStream()
        {
            if (!_isStreaming)
            {
                Debug.LogWarning("流式识别未运行");
                return;
            }

            try
            {
                Debug.Log("⏹️ 停止流式识别...");

                if (_stream != null)
                {
                    _stream.OnSegmentUpdated -= OnStreamSegmentUpdated;
                    _stream.OnSegmentFinished -= OnStreamSegmentFinished;
                    _stream.OnStreamFinished -= OnStreamFinished;

                    _stream.StopStream();
                    _stream = null;
                }

                if (_microphoneRecord != null && _microphoneRecord.IsRecording)
                {
                    _microphoneRecord.StopRecord();
                }

                _isStreaming = false;

                Debug.Log("✅ 流式识别已停止");
                Debug.Log($"📝 完整识别结果:\n{_accumulatedText}");

                OnStreamStopped?.Invoke();
                OnFinalResult?.Invoke(_accumulatedText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"停止流式识别失败: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 流片段更新回调（实时更新，可能会变化）
        /// </summary>
        private void OnStreamSegmentUpdated(WhisperResult result)
        {
            if (result == null) return;

            var text = result.Result?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 预处理文本
            text = ProcessText(text);

            if (printRealtimeResults)
            {
                Debug.Log($"[实时] {text}");
            }
        }

        /// <summary>
        /// 流片段完成回调（最终确定的文本）
        /// </summary>
        private void OnStreamSegmentFinished(WhisperResult result)
        {
            if (result == null) return;

            var text = result.Result?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 预处理文本
            text = ProcessText(text);

            Debug.Log($"[确定] {text}");
            OnSegmentReceived?.Invoke(text);
        }

        /// <summary>
        /// 流结束回调
        /// </summary>
        private void OnStreamFinished(string finalResult)
        {
            Debug.Log($"📝 流识别完成: {finalResult}");
            _accumulatedText = finalResult; // 使用最终结果
            StopStream();
        }

        /// <summary>
        /// 处理识别的文本并检测敏感词
        /// </summary>
        private string ProcessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            if (status) status.text = text;

            text = text.Trim();
            if (text.Contains("我草") || text.Contains("Fuck") || text.Contains("What's up") || text.Contains("化客") ||
                text.Contains("罚客") || text.Contains("bucker") || text.Contains("buck") || text.Contains("fucker") ||
                text.Contains("我靠") || text.Contains("妈") || text.Contains("大爷") || text.Contains("日") ||
                text.Contains("操")
               )
            {
                print("素质有待提高");
                _accumulatedText = "素质有待提高";
                EventBus.TriggerEvent(new DirtyTalkEvent());
            }

            if (!_eggTriggered && (text.Contains("bug") || text.Contains("八个") || text.Contains("八個") || text.Contains("霸格")) &&
                (text.Contains("確定") || text.Contains("确定") || text.Contains("这") || text.Contains("這")))
            {
                print("you win");
                _accumulatedText = "you win";
                _eggTriggered = true;
                EventBus.TriggerEvent(new EggTalkEvent());
            }

            return text;
        }

        /// <summary>
        /// 获取当前累积的文本
        /// </summary>
        public string GetAccumulatedText()
        {
            return _accumulatedText;
        }

        private void OnDestroy()
        {
            if (_isStreaming)
            {
                StopStream();
            }
        }

        /// <summary>
        /// 当前是否正在流式识别
        /// </summary>
        public bool IsStreaming => _isStreaming;

        /// <summary>
        /// 获取模型加载状态
        /// </summary>
        public bool IsModelLoaded => whisperManager != null && whisperManager.IsLoaded;
    }
}