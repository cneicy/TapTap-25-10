using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CupBox : Singleton<CupBox>
    {
        public List<string> cupsPlayerHad = new();
        public List<CupBase> allCups = new();
        
        
        private void OnEnable()
        {
            allCups = GetComponentsInChildren<CupBase>().ToList();
            foreach (var cupBase in allCups)
            {
                cupBase.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            print($"Just print {DataManager.Instance} for init");
        }

        [EventSubscribe]
        public void OnPlayerDataLoadedEvent(PlayerDataLoadedEvent evt)
        {
            if(DataManager.Instance.GetData<List<string>>("cupsPlayerHad") is not null)
                cupsPlayerHad = DataManager.Instance.GetData<List<string>>("cupsPlayerHad");
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

        [EventSubscribe]
        public void OnPlayerGetCupEvent(PlayerGetCupEvent evt)
        {
            if (!cupsPlayerHad.Contains(evt.Cup.Name))
            {
                cupsPlayerHad.Add(evt.Cup.Name);
                DataManager.Instance.SetData("cupsPlayerHad", cupsPlayerHad);
            }

            RefreshCups();
        }
    }
}