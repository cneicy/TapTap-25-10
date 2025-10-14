using ShrinkEventBus;

namespace Game.Level
{
    public class LevelChangeEvent : EventBase
    {
        public string LevelName;
        public bool IsTrueWorld { get; set; }

        public LevelChangeEvent(string levelName, bool isTrueWorld)
        {
            LevelName = levelName;
            IsTrueWorld = isTrueWorld;
        }
    }
}