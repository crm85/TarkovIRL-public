using RealismMod;

namespace TarkovIRL
{
    internal static class RaidState
    {
        public static bool IsInRaid
        {
            get { return GameWorldController.TimeInRaid > 0; }
        }
    }
}
