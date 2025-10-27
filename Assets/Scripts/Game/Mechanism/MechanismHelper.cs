// MechanismHelpers.cs

namespace Game.Mechanism
{
    public static class MechanismHelpers
    {
        public static void StartByConfigured(MechanismBase m)
        {
            if (!m) return;
            switch (m.mode)
            {
                case MotionMode.Once:         m.StartOnce(m.pauseAtEnds); break;
                case MotionMode.PingPongOnce: m.StartPingPongOnce(m.pauseAtEnds); break;
                case MotionMode.Loop:         m.StartLoop(m.pauseAtEnds); break;
                default:                      m.StartOnce(m.pauseAtEnds); break;
            }
        }
    }
}