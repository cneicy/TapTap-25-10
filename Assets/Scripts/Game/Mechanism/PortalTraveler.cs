using UnityEngine;

namespace Game.Mechanism
{
    public class PortalTraveler : MonoBehaviour
    {
        private float _immuneUntil;

        public void GiveArrivalImmunity(float seconds)
        {
            _immuneUntil = Time.time + Mathf.Max(0f, seconds);
        }

        public bool IsImmune => Time.time < _immuneUntil;
    }
}