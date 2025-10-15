using System;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Cup
{
    public class CupItem : MonoBehaviour
    {
        private CupBase _cup;
        private void Awake()
        {
            _cup = GetComponent<CupBase>();
        }

        private void OnEnable()
        {
            // todo:此处逻辑应该转移到关卡场景切换处
            if (CupBox.Instance.cupsPlayerHad.Contains(_cup.Name))
                gameObject.SetActive(false);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                EventBus.TriggerEvent(new PlayerGetCupEvent(_cup));
                gameObject.SetActive(false);
            }
        }
    }
}