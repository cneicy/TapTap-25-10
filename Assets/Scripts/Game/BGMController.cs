using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Game
{
    public class BGMController : Singleton<BGMController>
    {
        [Header("音源设置")]
        public AudioSource bgmSource;

        [Header("默认音量")]
        [Range(0f, 1f)] public float defaultBGMVolume = 1f;

        [System.Serializable]
        public struct Sound
        {
            public string name;
            public AudioClip clip;
        }

        [Header("音效/音乐资源列表")]
        public List<Sound> sounds = new();

        private Coroutine _bgmFadeCoroutine;
        private string _currentBGMName;

        protected override void Awake()
        {
            base.Awake();

            if (!bgmSource)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
        }

        public void PlayBGM(string name, float fadeTime = 1f)
        {
            var s = sounds.Find(x => x.name == name);
            if (!s.clip) return;

            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);

            _bgmFadeCoroutine = StartCoroutine(FadeInBGM(s, fadeTime));
        }

        public void StopBGM(float fadeTime = 1f)
        {
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);

            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
        }

        public void ChangeBGMVolume(float targetVolume, float fadeTime = 0.5f)
        {
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);

            _bgmFadeCoroutine = StartCoroutine(FadeVolumeBGM(targetVolume, fadeTime));
        }

        private IEnumerator FadeInBGM(Sound sound, float fadeTime)
        {
            if (bgmSource.isPlaying)
                yield return FadeOutBGM(fadeTime * 0.5f);

            bgmSource.clip = sound.clip;
            _currentBGMName = sound.name;
            bgmSource.Play();

            var timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, defaultBGMVolume, timer / fadeTime);
                yield return null;
            }

            bgmSource.volume = defaultBGMVolume;
        }

        public bool IsPlaying(string name)
        {
            if (!bgmSource.isPlaying)
                return false;
            if (string.IsNullOrEmpty(_currentBGMName))
                return false;
            return _currentBGMName == name;
        }

        private IEnumerator FadeOutBGM(float fadeTime)
        {
            var startVol = bgmSource.volume;
            var timer = 0f;

            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / fadeTime);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = 0f;
            _currentBGMName = null;
        }

        private IEnumerator FadeVolumeBGM(float target, float fadeTime)
        {
            var startVol = bgmSource.volume;
            var timer = 0f;

            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, target, timer / fadeTime);
                yield return null;
            }

            bgmSource.volume = target;
        }
    }
}
