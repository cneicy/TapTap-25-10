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
            
            var voiceId = GetComponent<LDtkFields>().GetString("voiceid");
            
            switch (voiceId)
            {
                case "meta i-3.4":
                    MetaAudioManager.Instance.Play("meta i-3.4");
                    SoundManager.Instance.Play("meta i-3.4");
                    DataManager.Instance.SetData("RevolverChainExplained", true, true);
                    DataManager.Instance.SetData("HasTriggeredRevolverChain", true, true);
                    break;
                case "meta i-3.2":
                    MetaAudioManager.Instance.Play("meta i-3.2");
                    SoundManager.Instance.Play("meta i-3.2");
                    DataManager.Instance.SetData("ParachuteFloatExplained", true, true);
                    DataManager.Instance.SetData("HasTriggeredParachuteFloat", true, true);
                    break;
                // todo:顺序很重要，兄弟
                case "meta 1-3.5" when DataManager.Instance.GetData<bool>("IsNotFirstTriggerTrap"):
                    MetaAudioManager.Instance.Play("meta 1-3.5.1");
                    SoundManager.Instance.Play("meta 1-3.5.1");
                    break;
                case "meta 1-3.5" when !DataManager.Instance.GetData<bool>("IsNotFirstTriggerTrap"):
                    DataManager.Instance.SetData("IsNotFirstTriggerTrap", true,true);
                    MetaAudioManager.Instance.Play(voiceId);
                    SoundManager.Instance.Play(voiceId);
                    break;
                default:
                    MetaAudioManager.Instance.Play(voiceId);
                    SoundManager.Instance.Play(voiceId);
                    break;
            }
                
            if(GetComponent<LDtkFields>().GetBool("disposable")) Destroy(gameObject);
        }
    }
}