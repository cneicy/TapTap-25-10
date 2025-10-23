using System;
using Game.Mechanism;
using LDtkUnity;
using UnityEngine;

namespace Game.Meta
{
    public class MetaPoint : MonoBehaviour
    {
        public TensionBar level;
        public ButtonBar button;
        
        private void OnEnable()
        {
            if(!GetComponent<LDtkFields>()) Debug.LogWarning("MetaPoint cannot find fields");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                level?.HandleCollider(other);
                button?.HandleCollider(other);
                MetaAudioManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
                if(GetComponent<LDtkFields>().GetBool("disposable")) Destroy(gameObject);
            }
        }
    }
}