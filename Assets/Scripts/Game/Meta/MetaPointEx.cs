using System;
using Data;
using Game.Mechanism;
using LDtkUnity;
using UnityEngine;

namespace Game.Meta
{
    public class MetaPointEx : MonoBehaviour
    {
        public TensionBar level;
        public ButtonBar button;
        public string customVoiceID;
        
        private void OnEnable()
        {
            if(!GetComponent<LDtkFields>()) Debug.LogWarning("MetaPoint cannot find fields");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || DataManager.Instance.GetData<bool>("IsLevel2LayerActive")) return;
            level?.HandleCollider(other);
            button?.HandleCollider(other);
            if(GetComponent<LDtkFields>())
            {
                MetaAudioManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
                if(GetComponent<LDtkFields>().GetBool("disposable")) Destroy(gameObject);
                else return;
            }
            else MetaAudioManager.Instance.Play(customVoiceID);
            Destroy(gameObject);
        }
    }
}