using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    public abstract class BulletPattern : ScriptableObject
    {
        public abstract IEnumerator PlayPattern(Transform origin, Transform player = null);
    }
}