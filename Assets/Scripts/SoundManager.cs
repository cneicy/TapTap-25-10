using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SoundManager : Singleton<SoundManager>
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
        var s = sounds.Find(x => x.name == name);
        if (!s.clip) return;
        if (s.clip.name.Contains("meta"))
        {
            source.Stop();
        }
        source.PlayOneShot(s.clip);
    }
}