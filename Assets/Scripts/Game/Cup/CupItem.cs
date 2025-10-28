using System.Collections;
using System.Collections.Generic;
using Data;
using Game.Level;
using ScreenEffect;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Cup
{
    public class CupItem : MonoBehaviour
    {
        private CupBase _cup;
        private bool _touched;
        private void Awake()
        {
            _cup = GetComponent<CupBase>();
        }

        private void OnEnable()
        {
            if(DataManager.Instance.GetData<List<string>>("CupsPlayerHad") is not null)
                if (DataManager.Instance.GetData<List<string>>("CupsPlayerHad").Contains(_cup.Name))
                {
                    gameObject.SetActive(false);
                }
            if (!GetComponentInParent<CupBox>()) return;
            if (GetComponentInParent<CupBox>().cupsPlayerHad.Contains(_cup.Name))
                gameObject.SetActive(false);
        }

        private IEnumerator OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if(_touched) yield break;
                _touched =  true;
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
                EventBus.TriggerEvent(new PlayerGetCupEvent(_cup));
                SoundManager.Instance.Play("庆贺吧1");
                yield return new WaitForSeconds(1f);
                SoundManager.Instance.Play("庆贺吧2");

                if (_cup.Name == "STGCup")
                {
                    RectTransitionController.Instance.StartTransition();
                    yield return new WaitForSeconds(0.25f);
                    LevelManager.Instance.SwitchLevel(DataManager.Instance.GetData<string>("CurrentLevel"));
                }else
                    gameObject.SetActive(false);
            }
        }
    }
}