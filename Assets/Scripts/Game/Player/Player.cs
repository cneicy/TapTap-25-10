using Game.Buff;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Player
{
    [EventBusSubscriber]
    public class Player : MonoBehaviour
    { 
        public Rigidbody2D rb2d;
        public float playerSpeedY1;
        public float playerSpeedY2;
        private BuffManager _buffManager;
        private PlayerController _playerController;

        public bool isWearGreySpringShoe = false;
        
        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            _buffManager = GetComponent<BuffManager>();
            _playerController = GetComponent<PlayerController>();
        }
        private void FixedUpdate()
        {
            WearGreySpringShoe();
        }

        private void WearGreySpringShoe()
        {
            if (isWearGreySpringShoe)
            {
                _playerController._stats.CoyoteTime = 15f;
            }
            else
            {
                _playerController._stats.CoyoteTime = 0.25f;
            }
        }
    }
}
