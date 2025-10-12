using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoiceToText;

namespace Game.VoiceToText
{
    /// <summary>
    /// 语音转文本UI控制器
    /// </summary>
    public class VoiceToTextUI : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("拖拽VoiceToTextManager组件到这里")]
        public VoiceToTextManager voiceManager;

        [Header("UI组件 - 按钮")]
        [Tooltip("开始录音按钮")]
        public Button recordButton;
        
        [Tooltip("停止录音按钮")]
        public Button stopButton;

        [Header("UI组件 - 文本")]
        [Tooltip("状态提示文本")]
        public TMP_Text statusText;
        
        [Tooltip("识别结果文本")]
        public TMP_Text resultText;

        [Header("UI组件 - 下拉框（可选）")]
        [Tooltip("麦克风选择下拉框，如果不需要可以留空")]
        public TMP_Dropdown microphoneDropdown;

        [Header("UI组件 - 进度条（可选）")]
        [Tooltip("录音进度条，如果不需要可以留空")]
        public Slider recordingProgressSlider;

        private float _recordingStartTime;

        private void Start()
        {
            // 检查引用
            if (voiceManager == null)
            {
                Debug.LogError("❌ 未设置 VoiceToTextManager 引用！请在Inspector中拖拽赋值。");
                return;
            }

            // 初始化UI
            InitializeUI();

            // 注册VoiceManager的事件
            voiceManager.OnRecordingStarted += OnRecordingStarted;
            voiceManager.OnRecordingStopped += OnRecordingStopped;
            voiceManager.OnRecognitionComplete += OnRecognitionComplete;
            voiceManager.OnRecognitionError += OnRecognitionError;

            // 绑定按钮点击事件
            if (recordButton != null)
            {
                recordButton.onClick.AddListener(OnRecordButtonClicked);
            }
            
            if (stopButton != null)
            {
                stopButton.onClick.AddListener(OnStopButtonClicked);
            }
            
            // 绑定下拉框事件
            if (microphoneDropdown != null)
            {
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        /// <summary>
        /// 初始化UI状态
        /// </summary>
        private void InitializeUI()
        {
            // 初始化麦克风下拉列表
            if (microphoneDropdown != null)
            {
                InitializeMicrophoneDropdown();
            }

            // 初始化按钮状态
            if (recordButton != null) recordButton.interactable = true;
            if (stopButton != null) stopButton.interactable = false;

            // 初始化文本
            if (statusText != null) statusText.text = "准备就绪";
            if (resultText != null) resultText.text = "等待语音输入...";

            // 初始化进度条
            if (recordingProgressSlider != null)
            {
                recordingProgressSlider.value = 0;
                recordingProgressSlider.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 初始化麦克风下拉列表
        /// </summary>
        private void InitializeMicrophoneDropdown()
        {
            microphoneDropdown.ClearOptions();
            var mics = voiceManager.GetAllMicrophones();

            if (mics.Length == 0)
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("❌ 未找到麦克风"));
                microphoneDropdown.interactable = false;
                
                if (recordButton != null) recordButton.interactable = false;
                if (statusText != null) statusText.text = "❌ 未检测到麦克风设备";
            }
            else
            {
                // 添加所有麦克风到下拉列表
                foreach (var mic in mics)
                {
                    microphoneDropdown.options.Add(new TMP_Dropdown.OptionData(mic));
                }
                
                microphoneDropdown.value = 0;
                microphoneDropdown.RefreshShownValue();
                microphoneDropdown.interactable = true;

                // 设置默认麦克风
                voiceManager.SetMicrophone(0);
                
                if (statusText != null) 
                {
                    statusText.text = $"已选择麦克风: {mics[0]}";
                }
            }
        }

        /// <summary>
        /// 录音按钮点击事件
        /// </summary>
        private void OnRecordButtonClicked()
        {
            voiceManager.StartRecording();
        }

        /// <summary>
        /// 停止按钮点击事件
        /// </summary>
        private void OnStopButtonClicked()
        {
            voiceManager.StopRecordingAndRecognize();
        }

        /// <summary>
        /// 麦克风选择改变事件
        /// </summary>
        private void OnMicrophoneChanged(int index)
        {
            voiceManager.SetMicrophone(index);
            
            if (statusText != null)
            {
                var mics = voiceManager.GetAllMicrophones();
                if (index >= 0 && index < mics.Length)
                {
                    statusText.text = $"已选择麦克风: {mics[index]}";
                }
            }
        }

        /// <summary>
        /// 录音开始回调
        /// </summary>
        private void OnRecordingStarted()
        {
            // 更新按钮状态
            if (recordButton != null) recordButton.interactable = false;
            if (stopButton != null) stopButton.interactable = true;
            if (microphoneDropdown != null) microphoneDropdown.interactable = false;

            // 更新UI文本
            if (statusText != null) statusText.text = "🎤 正在录音...";
            if (resultText != null) resultText.text = "";

            // 显示进度条
            if (recordingProgressSlider != null)
            {
                recordingProgressSlider.value = 0;
                recordingProgressSlider.gameObject.SetActive(true);
            }

            _recordingStartTime = Time.time;
        }

        /// <summary>
        /// 录音停止回调
        /// </summary>
        private void OnRecordingStopped()
        {
            // 更新按钮状态
            if (stopButton != null) stopButton.interactable = false;

            // 更新UI文本
            if (statusText != null) statusText.text = "⏳ 正在识别语音...";

            // 隐藏进度条
            if (recordingProgressSlider != null)
            {
                recordingProgressSlider.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 识别完成回调
        /// </summary>
        private void OnRecognitionComplete(string result)
        {
            // 恢复按钮状态
            if (recordButton != null) recordButton.interactable = true;
            if (microphoneDropdown != null) microphoneDropdown.interactable = true;

            // 更新UI文本
            if (statusText != null) statusText.text = "✅ 识别完成";
            if (resultText != null)
            {
                resultText.text = string.IsNullOrEmpty(result) ? "未识别到内容" : result;
            }

            Debug.Log($"[VoiceToText] 识别结果: {result}");
        }

        /// <summary>
        /// 识别错误回调
        /// </summary>
        private void OnRecognitionError(string error)
        {
            // 恢复按钮状态
            if (recordButton != null) recordButton.interactable = true;
            if (stopButton != null) stopButton.interactable = false;
            if (microphoneDropdown != null) microphoneDropdown.interactable = true;

            // 更新UI文本
            if (statusText != null) statusText.text = $"❌ 错误: {error}";
            if (resultText != null) resultText.text = "";

            // 隐藏进度条
            if (recordingProgressSlider != null)
            {
                recordingProgressSlider.value = 0;
                recordingProgressSlider.gameObject.SetActive(false);
            }

            Debug.LogError($"[VoiceToText] 错误: {error}");
        }

        /// <summary>
        /// 更新录音进度条
        /// </summary>
        private void Update()
        {
            // 如果正在录音，更新进度条
            if (voiceManager != null && voiceManager.IsRecording && recordingProgressSlider != null)
            {
                var elapsed = Time.time - _recordingStartTime;
                var progress = Mathf.Clamp01(elapsed / voiceManager.maxRecordingSeconds);
                recordingProgressSlider.value = progress;
            }
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        private void OnDestroy()
        {
            if (voiceManager != null)
            {
                voiceManager.OnRecordingStarted -= OnRecordingStarted;
                voiceManager.OnRecordingStopped -= OnRecordingStopped;
                voiceManager.OnRecognitionComplete -= OnRecognitionComplete;
                voiceManager.OnRecognitionError -= OnRecognitionError;
            }

            // 清理按钮事件
            if (recordButton != null)
            {
                recordButton.onClick.RemoveListener(OnRecordButtonClicked);
            }
            
            if (stopButton != null)
            {
                stopButton.onClick.RemoveListener(OnStopButtonClicked);
            }
            
            if (microphoneDropdown != null)
            {
                microphoneDropdown.onValueChanged.RemoveListener(OnMicrophoneChanged);
            }
        }
    }
}