using System;
using Game.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Menu
{
    public class PauseMenu : Singleton<PauseMenu>
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SceneManager.GetActiveScene().name == "PauseMenu")
                {
                    SceneManager.UnloadSceneAsync("PauseMenu");
                    FindAnyObjectByType<Game.Player.PlayerController>().enabled = true;
                }
                else
                {
                    SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
                    FindAnyObjectByType<Game.Player.PlayerController>().enabled = false;
                }
            }
        }
    }
}