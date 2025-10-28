using System.Threading.Tasks;
using Data;
using Game.Meta;
using Game.VoiceToText;
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
            SoundManager.Instance.Play("levelloaded");
            CurrentLevel = evt.LevelName;
        }

        [EventSubscribe]
        public async Task OnGameStartEvent(GameStartEvent evt)
        {
            if (CurrentLevel is not null)
            {
                await SwitchLevel(CurrentLevel);
            }
            else await SwitchLevel("StartMenu");
        }

        [EventSubscribe]
        public void OnEggTalkEvent(EggTalkEvent evt)
        {
            SwitchLevel("Level_Voice");
        }

        [EventSubscribe]
        public void OnDirtyTalkEvent(DirtyTalkEvent evt)
        {
            MetaAudioManager.Instance.Play("noshuzhi");
        }

        public async Task SwitchLevel(string levelName)
        {
            SoundManager.Instance.Play("switchlevel");
            if (levelName is "" or null)
            {
                await SceneManager.LoadSceneAsync("StartMenu");
            }
            switch (levelName)
            {
                case "Level_Voice" or "FakeStartMenu":
                    BGMController.Instance.StopBGM(4);
                    break;
                case "Level_STG":
                    BGMController.Instance.PlayBGM("STG");
                    break;
                case "Acknowledgements":
                    BGMController.Instance.PlayBGM("Acknowledgments");
                    break;
                default:
                    if(levelName.StartsWith("Level1"))
                    {
                        if (BGMController.Instance.IsPlaying("jazz")) break;
                        BGMController.Instance.PlayBGM("jazz");
                    }

                    if (levelName.StartsWith("Leveli") || levelName.StartsWith("Level2"))
                    {
                        if (BGMController.Instance.IsPlaying("New")) break;
                        BGMController.Instance.PlayBGM("New");
                    }
                    break;
            }

            await SceneManager.LoadSceneAsync(levelName);
        }
    }
}