using System;

namespace Game.Item
{
    public class Hands : ItemBase
    {
        private void Awake()
        {
            Name = "Hands";
            WindupDuration = 0.2f;
            RecoveryDuration = 0.2f;
        }
    }
}