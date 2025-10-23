using System.Collections.Generic;
using Data;
using ScreenEffect;
using ShrinkEventBus;
using UnityEngine;
public class GameStartEvent : EventBase{}
public class LoadCupsEvent : EventBase{}
public class LoadLevelsEvent : EventBase{}
public class LoadItemsEvent : EventBase{}

public class Entry :  MonoBehaviour
{
    [SerializeField] private GameObject levelManager;
    [SerializeField] private GameObject[] uiShouldBeDisabled;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject itemPanel;
    private void Start()
    {
        itemPanel.SetActive(false);
        if (DataManager.Instance.GetData<bool>("IsNotFirstStart"))
        {
            StartGame();
        }
        else
        {
            DataManager.Instance.SetData("IsNotFirstStart", true);
            DataManager.Instance.SetData("MicrophoneEnabled", true);
            levelManager.SetActive(false);
        }
    }

    public async void StartGame()
    {
        levelManager.SetActive(true);
        itemPanel.SetActive(true);
        foreach (var temp in uiShouldBeDisabled)
        {
            temp.gameObject.SetActive(false);
        }
        try
        {
            if(DataManager.Instance.GetData<List<string>>("CupsPlayerHad") is not null)
                await EventBus.TriggerEventAsync(new LoadCupsEvent()); 
            if(DataManager.Instance.GetData<string>("CurrentLevel") != "")
                await EventBus.TriggerEventAsync(new LoadLevelsEvent());
            if(DataManager.Instance.GetData<List<string>>("ItemsPlayerHad") is not null)
                await EventBus.TriggerEventAsync(new LoadItemsEvent());
            
            await EventBus.TriggerEventAsync(new GameStartEvent());
            RectTransitionController.Instance.StartTransition(
                onTransitionMiddle: async () => {
                    panel.SetActive(false);
                },
                onTransitionComplete: async () => {
                    
                }
            );
        }
        catch
        {
            // ignored
        }
    }
}