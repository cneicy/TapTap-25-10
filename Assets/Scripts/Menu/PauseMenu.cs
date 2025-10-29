using Data;
using Game.Cup;
using Game.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Menu
{
    public class PauseMenu : Singleton<PauseMenu>
    {
        private int _firstTimePauseCount;
        
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var isNotFirstTime = DataManager.Instance.GetData<bool>("IsNotFirstTimePause");

            if (!isNotFirstTime)
            {
                _firstTimePauseCount++;
                if (_firstTimePauseCount == 1)
                {
                    MetaAudioManager.Instance.Play("nopause");
                }

                SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
                var player = FindAnyObjectByType<Game.Player.PlayerController>();
                if (player) player.enabled = false;

                if (_firstTimePauseCount < 3) return;
                DataManager.Instance.SetData("IsNotFirstTimePause", true, true);
                MetaAudioManager.Instance.Play("nomorepause");

                return;
            }

            if (SceneManager.GetSceneByName("PauseMenu").isLoaded)
            {
                SceneManager.UnloadSceneAsync("PauseMenu");
                var player = FindAnyObjectByType<Game.Player.PlayerController>();
                if (player) player.enabled = true;

                var dialog = FindAnyObjectByType<CupDescriptionDialog>();
                if (dialog)
                    dialog.rectTransform.gameObject.SetActive(false);
            }
            else
            {
                SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
                var player = FindAnyObjectByType<Game.Player.PlayerController>();
                if (player) player.enabled = false;
            }
        }
    }
}