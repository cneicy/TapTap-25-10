using UnityEngine;
using System.Collections.Generic;

namespace Game.STG.BulletHell
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;
        public AudioSource source;

        [System.Serializable]
        public struct Sound
        {
            public string name;
            public AudioClip clip;
        }

        public List<Sound> sounds = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Play(string name)
        {
            var s = sounds.Find(x => x.name == name);
            if (s.clip != null)
                source.PlayOneShot(s.clip);
        }
    }
}