using System;
using System.Threading.Tasks;
using Data;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Game.Level
{
    [EventBusSubscriber]
    public class LevelManager : Singleton<LevelManager>
    {
        public string CurrentLevel { get; private set; }

        [EventSubscribe]
        public async Task OnLoadLevelsEvent(LoadLevelsEvent evt)
        {
            CurrentLevel = DataManager.Instance.GetData<string>("CurrentLevel");
        }

        [EventSubscribe]
        public void OnLevelLoadedEvent(LevelLoadedEvent evt)
        {
            CurrentLevel = evt.LevelName;
        }

        [EventSubscribe]
        public async Task OnGameStartEvent(GameStartEvent evt)
        {
            if (CurrentLevel is not null)
            {
                await SwitchLevel(CurrentLevel);
            }
            else await SwitchLevel("Level1");
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.RightShift))
            {
                SwitchLevel("Level_STG");
            }
        }

        public async Task SwitchLevel(string levelName)
        {
            await SceneManager.LoadSceneAsync(levelName);
        }
    }
}