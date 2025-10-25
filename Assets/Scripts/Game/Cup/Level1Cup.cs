using System.Collections;
using Game.Level;
using ScreenEffect;
using UnityEngine;

namespace Game.Cup
{
    public class Level1Cup : CupBase
    {
        public Level1Cup()
        {
            Name = "Level1Cup";
            Description = "嘻嘻嘻嘻";
        }

        private Coroutine _contactCoroutine;

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.name.Contains("Player"))
            {
                _contactCoroutine = StartCoroutine(ContactTimer(other.gameObject));
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (!other.gameObject.name.Contains("Player")) return;
            if (_contactCoroutine == null) return;
            StopCoroutine(_contactCoroutine);
            _contactCoroutine = null;
        }

        private IEnumerator ContactTimer(GameObject player)
        {
            var elapsed = 0f;
            const float requiredTime = 3f;

            while (elapsed < requiredTime)
            {
                if (!IsPlayerStillColliding(player))
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            RectTransitionController.Instance.StartTransition();
            var dialog = FindAnyObjectByType<CupDescriptionDialog>();
            if (dialog)
                dialog.rectTransform.gameObject.SetActive(false);

            LevelManager.Instance.SwitchLevel("Level1-1");
        }

        private bool IsPlayerStillColliding(GameObject player)
        {
            var contacts = new Collider2D[5];
            var count = GetComponent<Collider2D>().Overlap(ContactFilter2D.noFilter, contacts);
            for (var i = 0; i < count; i++)
            {
                if (contacts[i] && contacts[i].gameObject == player)
                    return true;
            }
            return false;
        }
    }
}
