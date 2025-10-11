using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private float _speedDif;
        
        public ParachuteBuff(float amount, float duration)
        {
            BuffName = "Parachute";
            _speedDif = amount;
            Duration = duration;
        }

        public override void OnApply(Player.Player target)
        {
            target.playerSpeedY+= _speedDif;
        }

        public override void OnRemove(Player.Player target)
        {
            target.playerSpeedY-= _speedDif;
        }
        
    }
}
