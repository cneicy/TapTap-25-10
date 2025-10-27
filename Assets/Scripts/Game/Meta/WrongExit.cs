using System.Collections;
using Game.Level;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Meta
{
    public class WrongExit : MonoBehaviour
    {
        private IEnumerator OnTriggerEnter2D(Collider2D other)
        {
            SoundManager.Instance.Play("btnclick");
            GetComponent<BoxCollider2D>().enabled = false;
            FindAnyObjectByType<ChattingMonitor>().gameObject.SetActive(false);
            foreach (var videoPlayer in FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None))
            {
                videoPlayer.Stop();
            }
            MetaAudioManager.Instance.Play("lol");
            yield return new WaitForSeconds(2f);
            MetaAudioManager.Instance.Play("wrongbtn");
            yield return new WaitForSeconds(2f);
            LevelManager.Instance.SwitchLevel(LevelManager.Instance.CurrentLevel);
        }
    }
}