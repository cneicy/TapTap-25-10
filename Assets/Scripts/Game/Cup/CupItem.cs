using System;
using UnityEngine;

namespace Game.Cup
{
    public class CupItem : MonoBehaviour
    {
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                
            }
        }
    }
}