using ShrinkEventBus;
using TMPro;
using UnityEngine;

namespace Game.VoiceToText
{
    [EventBusSubscriber]
    public class TestText : MonoBehaviour
    {
        [EventSubscribe]
        public void OnDirtyTalkEvent(DirtyTalkEvent evt)
        {
            GetComponent<TMP_Text>().text = "Dir";
        }

        [EventSubscribe]
        public void OnEggTalkEvent(EggTalkEvent evt)
        {
            GetComponent<TMP_Text>().text = "Egg";
        }
    }
}