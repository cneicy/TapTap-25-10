namespace Game.Item
{
    public class TestItem : ItemBase
    {
        private void Awake()
        {
            Name = "TestItem";
            WindupDuration = 0.2f;
            RecoveryDuration = 0.2f;
            Duration = 1f;
            Cooldown = 2f;
        }
    }
}