using UnityEngine;
using UnityEngine.UI;

namespace Game.STG.BulletHell
{
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public void SetHealth(float current, float max)
        {
            slider.value = current / max;
        }
    }
}