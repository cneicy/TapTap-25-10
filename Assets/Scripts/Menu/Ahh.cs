using Data;
using UnityEngine;

namespace Menu
{
    public class Ahh : MonoBehaviour
    {
        public void Exit()
        {
            Application.Quit();
        }

        public void ResetData()
        {
            DataManager.Instance.ResetData();
            Exit();
        }
    }
}