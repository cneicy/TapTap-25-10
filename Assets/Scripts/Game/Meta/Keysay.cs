using UnityEngine;

namespace Game.Meta
{
    public class Keysay : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                MetaAudioManager.Instance.Play("keysay");
            }
        }
    }
}