using System;
using System.Collections.Generic;
using System.Linq;
using Data;
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
            if (DataManager.Instance.GetData<List<string>>("CupsPlayerHad").Contains(_cup.Name))
            {
                gameObject.SetActive(false);
            }
            if (!GetComponentInParent<CupBox>()) return;
            if (GetComponentInParent<CupBox>().cupsPlayerHad.Contains(_cup.Name))
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                EventBus.TriggerEvent(new PlayerGetCupEvent(_cup));
                SoundManager.Instance.Play("庆贺吧");
                var temp = DataManager.Instance.GetData<List<string>>("CupsPlayerHad");
                if (temp is not null)
                {
                    if (!temp.Contains(_cup.Name))
                    {
                        temp.Add(_cup.Name);
                        DataManager.Instance.SetData("CupsPlayerHad", temp, true);
                    }
                }
                else
                {
                    temp = new List<string> { _cup.Name };
                    DataManager.Instance.SetData("CupsPlayerHad", temp, true);
                }

                gameObject.SetActive(false);
            }
        }
    }
}