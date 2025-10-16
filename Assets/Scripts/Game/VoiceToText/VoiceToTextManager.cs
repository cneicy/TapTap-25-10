using System;
using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace VoiceToText
{
    /// <summary>
    /// Unity 语音转文本管理器 (使用 whisper.unity)
    /// 使用麦克风录音并通过 Whisper 转换为文本
    /// 
    /// 使用说明：
    /// 1. 在场景中创建一个GameObject
    /// 2. 添加 WhisperManager 组件，并在Inspector中配置模型路径
    /// 3. 添加本组件 VoiceToTextManager
    /// 4. 在Inspector中将 WhisperManager 拖拽到 whisperManager 字段
    /// </summary>
    public class VoiceToTextManager : MonoBehaviour
    {
        [Header("组件引用")]
        [Tooltip("拖拽场景中的 WhisperManager 组件到这里")]
        public WhisperManager whisperManager;
        
        [Header("录音配置")]
        [Tooltip("最大录音时长(秒)")]
        public int maxRecordingSeconds = 30;
        
        [Tooltip("采样率(Hz) - whisper.unity推荐16000")]
        public int sampleRate = 16000;
        
        [Tooltip("音频增益，提高音量可能改善识别")]
        [Range(1f, 3f)]
        public float audioGain = 1.5f;
        
        [Tooltip("启用降噪处理")]
        public bool enableNoiseReduction = true;
        
        [Tooltip("噪音阈值")]
        [Range(0f, 0.1f)]
        public float noiseThreshold = 0.01f;
        
        [Header("麦克风设置")]
        [Tooltip("麦克风设备索引，-1为默认设备")]
        public int microphoneDeviceIndex = -1;

        // 事件回调
        public event Action<string> OnRecognitionComplete;
        public event Action<string> OnRecognitionError;
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;

        // 组件
        private MicrophoneRecord _microphoneRecord;
        private string _currentMicDevice;
        private bool _isRecording;

        private void Awake()
        {
            // 检查必要的引用
            if (whisperManager == null)
            {
                Debug.LogError("❌ 未设置 WhisperManager 引用！请在场景中添加 WhisperManager 组件并拖拽到 Inspector 中。");
                return;
            }
            
            // 初始化麦克风录音组件
            InitializeMicrophoneRecord();
        }

        private void Start()
        {
            // 初始化麦克风设备
            InitializeMicrophone();
        }

        /// <summary>
        /// 初始化麦克风录音组件
        /// </summary>
        private void InitializeMicrophoneRecord()
        {
            _microphoneRecord = gameObject.AddComponent<MicrophoneRecord>();
            
            // 设置录音参数
            _microphoneRecord.frequency = sampleRate;
            _microphoneRecord.maxLengthSec = maxRecordingSeconds;
            _microphoneRecord.loop = false;
            
            // 注册录音完成事件
            _microphoneRecord.OnRecordStop += OnMicrophoneRecordStop;
        }

        /// <summary>
        /// 初始化麦克风设备
        /// </summary>
        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("未检测到麦克风设备！");
                OnRecognitionError?.Invoke("未检测到麦克风设备");
                return;
            }

            // 选择麦克风设备
            if (microphoneDeviceIndex >= 0 && microphoneDeviceIndex < Microphone.devices.Length)
            {
                _currentMicDevice = Microphone.devices[microphoneDeviceIndex];
            }
            else
            {
                _currentMicDevice = null; // null表示使用默认设备
            }

            _microphoneRecord.SelectedMicDevice = _currentMicDevice;
            Debug.Log($"已选择麦克风: {(_currentMicDevice ?? "默认设备")}");
        }

        /// <summary>
        /// 获取所有可用麦克风设备名称
        /// </summary>
        public string[] GetAllMicrophones()
        {
            return Microphone.devices;
        }

        /// <summary>
        /// 设置使用的麦克风设备
        /// </summary>
        public void SetMicrophone(int index)
        {
            if (index >= 0 && index < Microphone.devices.Length)
            {
                microphoneDeviceIndex = index;
                _currentMicDevice = Microphone.devices[index];
                _microphoneRecord.SelectedMicDevice = _currentMicDevice;
                Debug.Log($"切换麦克风: {_currentMicDevice}");
            }
            else if (index == -1)
            {
                microphoneDeviceIndex = -1;
                _currentMicDevice = null;
                _microphoneRecord.SelectedMicDevice = null;
                Debug.Log("切换到默认麦克风");
            }
        }

        /// <summary>
        /// 开始录音
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording)
            {
                Debug.LogWarning("已经在录音中");
                return;
            }

            if (_microphoneRecord == null)
            {
                Debug.LogError("麦克风录音组件未初始化");
                OnRecognitionError?.Invoke("麦克风录音组件未初始化");
                return;
            }

            // 开始录音
            _microphoneRecord.StartRecord();
            _isRecording = true;
            
            Debug.Log("开始录音...");
            OnRecordingStarted?.Invoke();
        }

        /// <summary>
        /// 停止录音并开始识别
        /// </summary>
        public void StopRecordingAndRecognize()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("未在录音中");
                return;
            }

            // 停止录音（会触发OnRecordStop事件）
            _microphoneRecord.StopRecord();
            _isRecording = false;

            Debug.Log("停止录音，准备识别...");
            OnRecordingStopped?.Invoke();
        }

        /// <summary>
        /// 麦克风录音停止回调
        /// </summary>
        private async void OnMicrophoneRecordStop(AudioChunk recordedAudio)
        {
            // 检查录音数据
            if (recordedAudio.Data == null || recordedAudio.Data.Length == 0)
            {
                var error = "录音失败，未捕获到音频数据";
                Debug.LogError(error);
                OnRecognitionError?.Invoke(error);
                return;
            }

            try
            {
                Debug.Log($"录音完成，数据长度: {recordedAudio.Length}秒");
                
                // 预处理音频
                var processedAudio = PreprocessAudio(recordedAudio.Data);
                
                Debug.Log("正在识别语音...");
                
                // 确保WhisperManager引用存在
                if (whisperManager == null)
                {
                    var error = "WhisperManager 引用为空";
                    Debug.LogError(error);
                    OnRecognitionError?.Invoke(error);
                    return;
                }
                
                // 确保模型已加载
                if (!whisperManager.IsLoaded && !whisperManager.IsLoading)
                {
                    Debug.Log("模型未加载，正在加载...");
                    await whisperManager.InitModel();
                }
                
                // 等待模型加载完成
                while (whisperManager.IsLoading)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
                
                if (!whisperManager.IsLoaded)
                {
                    var error = "模型加载失败";
                    Debug.LogError(error);
                    OnRecognitionError?.Invoke(error);
                    return;
                }
                
                // 使用Whisper识别（使用预处理后的音频）
                var result = await whisperManager.GetTextAsync(
                    processedAudio, 
                    recordedAudio.Frequency, 
                    recordedAudio.Channels
                );
                
                if (result == null)
                {
                    var error = "识别失败，未返回结果";
                    Debug.LogError(error);
                    OnRecognitionError?.Invoke(error);
                    return;
                }

                // 提取识别文本
                var transcription = result.Result;
                
                if (string.IsNullOrEmpty(transcription))
                {
                    Debug.LogWarning("识别结果为空");
                    transcription = "[未识别到内容]";
                }
                
                Debug.Log($"识别结果: {transcription}");
                OnRecognitionComplete?.Invoke(transcription);
            }
            catch (Exception ex)
            {
                var error = $"识别失败: {ex.Message}";
                Debug.LogError(error);
                Debug.LogException(ex);
                OnRecognitionError?.Invoke(error);
            }
        }

        /// <summary>
        /// 预处理音频数据，提升识别质量
        /// </summary>
        private float[] PreprocessAudio(float[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return audioData;

            var processed = new float[audioData.Length];
            Array.Copy(audioData, processed, audioData.Length);

            // 1. 应用增益
            if (audioGain != 1f)
            {
                for (var i = 0; i < processed.Length; i++)
                {
                    processed[i] *= audioGain;
                    // 防止削波
                    processed[i] = Mathf.Clamp(processed[i], -1f, 1f);
                }
            }

            // 2. 降噪处理
            if (enableNoiseReduction)
            {
                processed = ApplyNoiseReduction(processed);
            }

            // 3. 归一化
            processed = NormalizeAudio(processed);

            return processed;
        }

        /// <summary>
        /// 简单的降噪处理
        /// </summary>
        private float[] ApplyNoiseReduction(float[] audioData)
        {
            var output = new float[audioData.Length];

            for (var i = 0; i < audioData.Length; i++)
            {
                // 简单的门限降噪
                if (Mathf.Abs(audioData[i]) < noiseThreshold)
                {
                    output[i] = 0f;
                }
                else
                {
                    output[i] = audioData[i];
                }
            }

            return output;
        }

        /// <summary>
        /// 归一化音频数据
        /// </summary>
        private float[] NormalizeAudio(float[] audioData)
        {
            // 找到最大振幅
            var maxAmplitude = 0f;
            for (var i = 0; i < audioData.Length; i++)
            {
                var abs = Mathf.Abs(audioData[i]);
                if (abs > maxAmplitude)
                    maxAmplitude = abs;
            }

            // 如果音频太小，进行归一化
            if (maxAmplitude > 0.01f && maxAmplitude < 0.8f)
            {
                var normalizeRatio = 0.8f / maxAmplitude;
                var normalized = new float[audioData.Length];
                for (var i = 0; i < audioData.Length; i++)
                {
                    normalized[i] = audioData[i] * normalizeRatio;
                }
                return normalized;
            }

            return audioData;
        }

        /// <summary>
        /// 直接识别AudioClip
        /// </summary>
        public async void RecognizeAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                OnRecognitionError?.Invoke("AudioClip为空");
                return;
            }

            if (whisperManager == null)
            {
                OnRecognitionError?.Invoke("WhisperManager 引用为空");
                return;
            }

            try
            {
                Debug.Log("正在识别AudioClip...");
                
                // 确保模型已加载
                if (!whisperManager.IsLoaded && !whisperManager.IsLoading)
                {
                    await whisperManager.InitModel();
                }
                
                while (whisperManager.IsLoading)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
                
                var result = await whisperManager.GetTextAsync(clip);
                
                if (result == null)
                {
                    OnRecognitionError?.Invoke("识别失败，未返回结果");
                    return;
                }

                var transcription = result.Result;
                Debug.Log($"识别结果: {transcription}");
                OnRecognitionComplete?.Invoke(transcription);
            }
            catch (Exception ex)
            {
                var error = $"识别失败: {ex.Message}";
                Debug.LogError(error);
                OnRecognitionError?.Invoke(error);
            }
        }

        /// <summary>
        /// 手动初始化模型
        /// </summary>
        public async void InitializeModel()
        {
            if (whisperManager == null)
            {
                Debug.LogError("WhisperManager 引用为空");
                return;
            }

            try
            {
                Debug.Log("开始加载Whisper模型...");
                await whisperManager.InitModel();
                Debug.Log("Whisper模型加载完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"模型加载失败: {ex.Message}");
                OnRecognitionError?.Invoke($"模型加载失败: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // 清理资源
            if (_isRecording && _microphoneRecord != null)
            {
                _microphoneRecord.StopRecord();
            }

            // 取消订阅事件
            if (_microphoneRecord != null)
            {
                _microphoneRecord.OnRecordStop -= OnMicrophoneRecordStop;
            }
        }

        /// <summary>
        /// 当前是否正在录音
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 模型是否已加载
        /// </summary>
        public bool IsModelLoaded => whisperManager != null && whisperManager.IsLoaded;

        /// <summary>
        /// 模型是否正在加载
        /// </summary>
        public bool IsModelLoading => whisperManager != null && whisperManager.IsLoading;

        /// <summary>
        /// 获取麦克风录音组件引用
        /// </summary>
        public MicrophoneRecord MicrophoneRecord => _microphoneRecord;
    }
}