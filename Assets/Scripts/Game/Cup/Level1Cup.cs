using System;
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

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.name.Contains("Player"))
            {
                RectTransitionController.Instance.StartTransition();
                if (FindAnyObjectByType<CupDescriptionDialog>())
                    FindAnyObjectByType<CupDescriptionDialog>().rectTransform.gameObject.SetActive(false);
                LevelManager.Instance.SwitchLevel("Level1");
            }
        }
    }
}