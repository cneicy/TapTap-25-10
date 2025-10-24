using System;
using System.Collections;
using System.Linq;
using Game.Level;
using Game.Player;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Item
{
    public class ItemInWorld : MonoBehaviour
    {
        private static readonly int Jump = Animator.StringToHash("Jump");
        public string theItem;

        private void OnEnable()
        {
            var manager = FindAnyObjectByType<LevelManager>();
            if (manager is null)
            {
                if (SceneManager.GetActiveScene().name != "Entry")
                {
                    SceneManager.LoadScene("Entry");
                    return;
                }
            }

            foreach (var unused in ItemSystem.Instance.ItemsPlayerHadTypeNames.Where(item => item == theItem))
            {
                gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            transform.Rotate(Vector3.up * (Time.fixedDeltaTime * 100), Space.Self);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            SoundManager.Instance.Play("pickup");

            EventBus.TriggerEvent(new PlayerSpinEvent(other.transform));
            EventBus.TriggerEvent(new PlayerGetItemEvent(theItem));
            
            gameObject.SetActive(false);
        }

    }
}
