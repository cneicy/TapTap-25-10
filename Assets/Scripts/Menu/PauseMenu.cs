using System;
using Game.Cup;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Menu
{
    public class PauseMenu : Singleton<PauseMenu>
    {
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (SceneManager.GetSceneByName("PauseMenu").isLoaded)
            {
                SceneManager.UnloadSceneAsync("PauseMenu");
                FindAnyObjectByType<Game.Player.PlayerController>().enabled = true;
                FindAnyObjectByType<CupDescriptionDialog>().rectTransform.gameObject.SetActive(false);
            }
            else
            {
                SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
                FindAnyObjectByType<Game.Player.PlayerController>().enabled = false;
            }
        }
    }
}