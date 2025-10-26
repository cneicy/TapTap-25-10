using System;
using UnityEngine;

namespace Game.Mechanism
{
    public class OneWay : MechanismBase
    {
        public override void StartProcess(Direction dirEnum, float spd, float dist, MotionMode m, float pauseEnds = 0)
        {
            base.StartProcess(dirEnum, spd, dist, m, pauseEnds);
            SoundManager.Instance.Play("blockmove");
        }

        public override void StartProcess(Vector2 direction, float spd, float dist, MotionMode m, float pauseEnds = 0)
        {
            base.StartProcess(direction, spd, dist, m, pauseEnds);
            SoundManager.Instance.Play("blockmove");
        }
    }
}
