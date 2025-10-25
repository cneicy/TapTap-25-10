using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using ShrinkEventBus;
using UnityEngine;
using Utils;

namespace Game.Cup
{
    public class PlayerGetCupEvent : EventBase
    {
        public CupBase Cup {  get; }
        public PlayerGetCupEvent(CupBase cup)
        {
            Cup = cup;
        }
    }
    [EventBusSubscriber]
    public class CupBox : MonoBehaviour
    {
        public List<string> cupsPlayerHad = new();
        public List<CupBase> allCups = new();
        protected void Awake()
        {
            allCups = GetComponentsInChildren<CupBase>().ToList();
            foreach (var cupBase in allCups)
            {
                cupBase.gameObject.SetActive(false);
            }
            cupsPlayerHad.Add("Level1Cup");
            RefreshCups();
        }

        [EventSubscribe]
        public async Task OnLoadCupsEvent(LoadCupsEvent evt)
        {
            if(DataManager.Instance.GetData<List<string>>("CupsPlayerHad") is not null)
            {
                cupsPlayerHad = DataManager.Instance.GetData<List<string>>("CupsPlayerHad");
                print(cupsPlayerHad);
            }
            RefreshCups();
        }
        
        public void RefreshCups()
        {
            foreach (var cupBase in allCups)
            {
                // 留意大小写，小写的是gameobject的name
                if (cupsPlayerHad.Contains(cupBase.Name))
                {
                    cupBase.gameObject.SetActive(true);
                }
                else
                {
                    cupBase.gameObject.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.name.Contains("Bullet")) return;
            foreach (var cup in allCups.Where(cup => cup.gameObject.activeSelf))
            {
                cup.Crash();
            }
        }

        [EventSubscribe]
        public void OnPlayerGetCupEvent(PlayerGetCupEvent evt)
        {
            if (!cupsPlayerHad.Contains(evt.Cup.Name))
            {
                cupsPlayerHad.Add(evt.Cup.Name);
                DataManager.Instance.SetData("CupsPlayerHad", cupsPlayerHad);
            }

            RefreshCups();
        }
    }
}