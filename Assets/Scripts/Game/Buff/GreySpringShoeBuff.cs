using Game.Player;

namespace Game.Buff
{
    public class GreySpringShoeBuff : BuffBase
    {
        private PlayerController _playerController;

        public GreySpringShoeBuff()
        {
            BuffName = "GreySpringShoe";
        }
        
        public override void OnApply(Player.Player target)
        {
            base.OnApply(target);
            _playerController = target.GetComponent<PlayerController>();
            target.isWearGreySpringShoe = true;
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            base.OnUpdate(target, deltaTime);
        }

        public override void OnRemove(Player.Player target)
        {
            base.OnRemove(target);
        }
    }
}
