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