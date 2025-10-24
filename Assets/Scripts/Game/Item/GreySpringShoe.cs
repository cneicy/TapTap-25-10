using Game.Player;
using JetBrains.Annotations;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class GreySpringShoe : ItemBase
    {
        public bool isUsed = false;
        [CanBeNull] private Player.Player _player;
        private static readonly int Jump = Animator.StringToHash("Jump");
        
        public GreySpringShoe()
        {
            Name = "灰色弹簧鞋";
            Description = "";
            WindupDuration = 0;
            Duration = 0;
            RecoveryDuration = 0;
            Cooldown = 0;
            IsBasement = true;
            IsBuff = false;
        }
        private void Awake()
        {
            isUsed = false;
            //ItemSystem.Instance.ItemsPlayerHad.Add(this);
        }

        public override void Start()
        {
            base.Start();
            Sprite = Resources.Load<Sprite>("Sprites/Items/GreySpringShoe");
        }

        public override void ApplyEffect()
        {
            _player = FindAnyObjectByType<Player.Player>();
            if (isUsed) return;
            SoundManager.Instance.Play("shoeuse");

            if (_player)
            {
                EventBus.TriggerEvent(new PlayerSpinEvent(_player.transform));
                _player.isWearGreySpringShoe = true;
            }

            isUsed = true;
            print(name +"已经使用");
        }
    }
}
