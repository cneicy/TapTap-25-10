using System;
using System.Collections.Generic;
using ShrinkEventBus;
using UnityEngine;
using Utils;

namespace Game.Cup
{
    public class PlayerGetCupEvent : EventBase
    {
        public CupBase Cup {  get; set; }
        public PlayerGetCupEvent(CupBase cup)
        {
            Cup = cup;
        }
    }
    [EventBusSubscriber]
    public class CupBox : Singleton<CupBox>
    {
        public List<string> cupsPlayerHad = new();
        
        
        
        private void OnEnable()
        {
            // todo:当玩家继续游戏时读取玩家数据拥有的奖杯
            cupsPlayerHad.Add("Tap Cup");
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                RefreshCups();
            }
        }

        public void RefreshCups()
        {
            foreach (var cupBase in GetComponentsInChildren<CupBase>())
            {
                // 留意大小写，小写的是gameobject的name
                if (!cupsPlayerHad.Contains(cupBase.Name))
                {
                    cupBase.gameObject.SetActive(false);
                }
            }
        }

        [EventSubscribe]
        public void OnPlayerGetCupEvent(PlayerGetCupEvent evt)
        {
            cupsPlayerHad.Add(evt.Cup.Name);
        }
    }
}