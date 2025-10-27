using System.Collections;
using UnityEngine;

namespace Game.Meta
{
    public class ChattingMonitor : MonoBehaviour
    {
        public AudioSource audioSource;
        private bool _isPlayFinish;
        private IEnumerator Start()
        {
            audioSource = GetComponent<AudioSource>();
            yield return new WaitForSeconds(2f);
            MetaAudioManager.Instance.Play("enterstudio");
        }
        

        private void Update()
        {
            if (!audioSource.isPlaying && !_isPlayFinish)
            {
                _isPlayFinish = true;
                MetaAudioManager.Instance.Play("finishchatting");
            }
        }
    }
}