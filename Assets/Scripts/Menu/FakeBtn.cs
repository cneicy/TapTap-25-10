using System;
using Game.Meta;
using UnityEngine;

namespace Menu
{
    public class FakeBtn : MonoBehaviour
    {
        private void OnMouseDown()
        {
            MetaAudioManager.Instance.Play("fakebtn");
        }
    }
}