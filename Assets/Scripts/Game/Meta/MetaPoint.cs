using Data;
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
            if (!other.CompareTag("Player") && !other.name.Contains("Bullet")) return;
            level?.HandleCollider(other);
            button?.HandleCollider(other);
            if (GetComponent<LDtkFields>().GetString("voiceid") == "meta 1-3.5")
            {
                if (!DataManager.Instance.GetData<bool>("IsNotFirstTriggerTrap"))
                {
                    DataManager.Instance.SetData("IsNotFirstTriggerTrap", true,true);
                    MetaAudioManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
                    SoundManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
                }
                else
                {
                    MetaAudioManager.Instance.Play("meta 1-3.5.1");
                    SoundManager.Instance.Play("meta 1-3.5.1");
                }
            }
            else
            {
                MetaAudioManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
                SoundManager.Instance.Play(GetComponent<LDtkFields>().GetString("voiceid"));
            }
                
            if(GetComponent<LDtkFields>().GetBool("disposable")) Destroy(gameObject);
        }
    }
}