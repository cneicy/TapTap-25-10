using UnityEngine;
using Utils;

namespace Menu
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private GameObject mainMenu;
        public void DisableMainMenu()
        {
            mainMenu.SetActive(false);
        }

        public void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }
    }
}