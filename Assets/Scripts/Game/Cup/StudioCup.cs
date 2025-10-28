using System.Collections;
using Game.Level;
using ScreenEffect;
using UnityEngine;

namespace Game.Cup
{
    public class StudioCup : CupBase
    {
        public StudioCup()
        {
            Name = "StudioCup";
            Description = "逸一时,误一世,逸久逸久罢已龄";
        }

        private Coroutine _contactCoroutine;
        private AudioSource _audioSource;
        private bool _isAudioPlaying;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.name.Contains("Player")) return;
            _contactCoroutine = StartCoroutine(ContactTimer(other.gameObject));
                
            StartAudio();
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (!other.gameObject.name.Contains("Player")) return;
            if (_contactCoroutine == null) return;
            
            StopCoroutine(_contactCoroutine);
            _contactCoroutine = null;
            
            StopAudio();
        }

        private IEnumerator ContactTimer(GameObject player)
        {
            var elapsed = 0f;
            const float requiredTime = 3f;

            while (elapsed < requiredTime)
            {
                if (!IsPlayerStillColliding(player))
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            OnContactComplete();
        }

        private void StartAudio()
        {
            if (_audioSource && !_isAudioPlaying)
            {
                _audioSource.Play();
                _isAudioPlaying = true;
            }
        }

        private void StopAudio()
        {
            if (_audioSource && _isAudioPlaying)
            {
                _audioSource.Stop();
                _isAudioPlaying = false;
                
                Debug.Log("停止播放接触音频");
            }
        }

        private void OnContactComplete()
        {
            StopAudio();
            
            RectTransitionController.Instance.StartTransition();
            var dialog = FindAnyObjectByType<CupDescriptionDialog>();
            if (dialog)
                dialog.rectTransform.gameObject.SetActive(false);

            LevelManager.Instance.SwitchLevel("Level_Voice");
        }

        private bool IsPlayerStillColliding(GameObject player)
        {
            var contacts = new Collider2D[5];
            var count = GetComponent<Collider2D>().Overlap(ContactFilter2D.noFilter, contacts);
            for (var i = 0; i < count; i++)
            {
                if (contacts[i] && contacts[i].gameObject == player)
                    return true;
            }
            return false;
        }
        
        [ContextMenu("测试音频播放")]
        private void TestAudioPlay()
        {
            StartAudio();
        }

        [ContextMenu("测试音频停止")]
        private void TestAudioStop()
        {
            StopAudio();
        }
    }
}