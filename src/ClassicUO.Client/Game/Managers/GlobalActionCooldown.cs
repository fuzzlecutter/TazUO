using ClassicUO.Configuration;

namespace ClassicUO.Game.Managers
{
    public static class GlobalActionCooldown
    {
        private static long nextActionTime = 0;
        private static long cooldownDuration => ProfileManager.CurrentProfile.MoveMultiObjectDelay;

        public static bool IsOnCooldown => Time.Ticks < nextActionTime;

        public static void ResetCooldown()
        {
            nextActionTime = Time.Ticks + cooldownDuration;
        }
    }
}