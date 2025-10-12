using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.VoiceToText
{
    /// <summary>
    /// 流式语音识别UI控制器
    /// </summary>
    public class VoiceStreamUI : MonoBehaviour
    {
        [Header("引用")]
        public VoiceStreamManager streamManager;

        [Header("UI组件")]
        public Button stopButton;
        public TMP_Text realtimeText;
        public TMP_Text accumulatedText;
        public TMP_Dropdown microphoneDropdown;

        [Header("显示设置")]
        public int maxRealtimeLength = 100;
        public Color streamingColor = Color.green;
        public Color idleColor = Color.gray;

        private void Start()
        {
            if (streamManager == null)
            {
                Debug.LogError("未设置 VoiceStreamManager 引用！");
                return;
            }

            InitializeUI();
            RegisterEvents();
        }

        private void InitializeUI()
        {
            // 初始化麦克风下拉列表
            if (microphoneDropdown != null)
            {
                InitializeMicrophoneDropdown();
            }
            
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopButtonClicked);

            // 初始状态
            UpdateButtonStates(false);
            if (realtimeText != null) realtimeText.text = "";
            if (accumulatedText != null) accumulatedText.text = "等待语音输入...";
        }

        private void InitializeMicrophoneDropdown()
        {
            microphoneDropdown.ClearOptions();
            var mics = streamManager.GetAllMicrophones();

            if (mics.Length == 0)
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("❌ 未找到麦克风"));
                microphoneDropdown.interactable = false;
            }
            else
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("默认麦克风"));
                foreach (var mic in mics)
                {
                    microphoneDropdown.options.Add(new TMP_Dropdown.OptionData(mic));
                }
                microphoneDropdown.value = 0;
                microphoneDropdown.RefreshShownValue();
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        private void RegisterEvents()
        {
            streamManager.OnStreamStarted += OnStreamStarted;
            streamManager.OnStreamStopped += OnStreamStopped;
            streamManager.OnSegmentReceived += OnSegmentReceived;
            streamManager.OnFinalResult += OnFinalResult;
            streamManager.OnError += OnError;
        }

        private void OnStopButtonClicked()
        {
            streamManager.StopStream();
        }

        private void OnMicrophoneChanged(int index)
        {
            streamManager.SetMicrophone(index - 1);
        }

        private void OnStreamStarted()
        {
            UpdateButtonStates(true);
            if (accumulatedText != null)
                accumulatedText.text = "";
        }

        private void OnStreamStopped()
        {
            UpdateButtonStates(false);
            if (realtimeText != null)
                realtimeText.text = "";
        }

        private void OnSegmentReceived(string text)
        {
            if (accumulatedText != null)
            {
                accumulatedText.text = streamManager.GetAccumulatedText();
                
                Canvas.ForceUpdateCanvases();
            }
        }

        private void OnFinalResult(string text)
        {
            if (accumulatedText != null)
            {
                accumulatedText.text = text;
            }
        }

        private void OnError(string error)
        {
            UpdateButtonStates(false);
        }

        private void UpdateButtonStates(bool isStreaming)
        {
            if (stopButton != null)
                stopButton.interactable = isStreaming;

            if (microphoneDropdown != null)
                microphoneDropdown.interactable = !isStreaming;
        }

        private void Update()
        {
            if (!streamManager.IsStreaming || !realtimeText) return;
            var currentText = streamManager.GetAccumulatedText();
            
            if (currentText.Length > maxRealtimeLength)
            {
                currentText = "..." + currentText.Substring(currentText.Length - maxRealtimeLength);
            }
                
            realtimeText.text = currentText;
        }

        private void OnDestroy()
        {
            if (streamManager != null)
            {
                streamManager.OnStreamStarted -= OnStreamStarted;
                streamManager.OnStreamStopped -= OnStreamStopped;
                streamManager.OnSegmentReceived -= OnSegmentReceived;
                streamManager.OnFinalResult -= OnFinalResult;
                streamManager.OnError -= OnError;
            }

            if (stopButton != null)
                stopButton.onClick.RemoveListener(OnStopButtonClicked);

            if (microphoneDropdown != null)
                microphoneDropdown.onValueChanged.RemoveListener(OnMicrophoneChanged);
        }
    }
}