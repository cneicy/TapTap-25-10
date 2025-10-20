using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Game.Meta
{
    public class MetaAudioManager : Singleton<MetaAudioManager>
    {
        public AudioSource source;

        [System.Serializable]
        public struct Sound
        {
            public string name;
            public AudioClip clip;
        }
        
        public List<Sound> sounds = new();
        
        public void Play(string name)
        {
            print("Playing " + name);
            var s = sounds.Find(x => x.name == name);
            if (s.clip)
                source.PlayOneShot(s.clip);
        }
    }
}