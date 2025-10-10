using ShrinkEventBus;
using TMPro;
using UnityEngine;

[EventBusSubscriber]
public class Test : MonoBehaviour
{
    public TMP_Text text;
    private void Start()
    {
        EventBus.TriggerEvent(new TestEvent());
    }

    [EventSubscribe]
    public void OnTestEvent(TestEvent ev)
    {
        text.text = ev+"\n"+"Fuck Unity";
    }
}