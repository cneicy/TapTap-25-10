using System;
using System.Collections;
using Data;
using Game.Player;
using UnityEngine;

namespace Game.Mechanism.Fuck
{
    public class Level6To2Lever : MonoBehaviour
    {
        private bool _isUnblock;
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            _isUnblock = DataManager.Instance.GetData<bool>("IsLevel2BlockUnBlock");
            if (_isUnblock)
            {
                GetComponent<TensionBar>()?.HandleCollider(FindAnyObjectByType<PlayerController>().gameObject.GetComponent<Collider2D>());
            }

            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
        }

        private IEnumerator OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _isUnblock = true;
                DataManager.Instance.SetData("IsLevel2BlockUnBlock", _isUnblock,true);
                SoundManager.Instance.Play("meclever");
                yield return new WaitForSeconds(0.1f);
                gameObject.SetActive(false);
            }
        }
    }
}