using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using ScreenEffect;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Game.Level
{
    [EventBusSubscriber]
    public class LevelManager : Singleton<LevelManager>
    {
        public List<string> Levels { get; } = new();
        public string CurrentLevel { get; private set; }

        [EventSubscribe]
        public async Task OnLoadLevelsEvent(LoadLevelsEvent evt)
        {
            CurrentLevel = DataManager.Instance.GetData<string>("CurrentLevel");
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

        public async Task SwitchLevel(string levelName)
        {
            await SceneManager.LoadSceneAsync(levelName);
        }
    }
}