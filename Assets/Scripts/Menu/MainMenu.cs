using UnityEngine;

namespace Menu
{
    public class MainMenu : MonoBehaviour
    {
        private void OnEnable()
        {
            UIManager.Instance.EnableMainMenu();
        }

        private void OnDisable()
        {
            UIManager.Instance.DisableMainMenu();
        }
    }
}