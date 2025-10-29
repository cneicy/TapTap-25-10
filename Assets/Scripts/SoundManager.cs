using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utils;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource source;
    
    private float _lastBlockMoveTime = -1f;
    private const float BlockmoveCooldown = 0.2f;

    [System.Serializable]
    public struct Sound
    {
        public string name;
        public AudioClip clip;
    }

    public List<Sound> sounds = new();

    public void Play(string name)
    {
        if (name == "blockmove")
        {
            if (Time.time - _lastBlockMoveTime < BlockmoveCooldown)
            {
                return;
            }
            _lastBlockMoveTime = Time.time;
        }

        var s = sounds.Find(x => x.name == name);
        if (!s.clip) return;

        source.PlayOneShot(s.clip);
    }
}