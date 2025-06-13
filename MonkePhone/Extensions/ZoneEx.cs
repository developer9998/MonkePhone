using System;

namespace MonkePhone.Extensions
{
    public static class ZoneEx
    {
        public static string ToTitleCase(this GTZone zone)
        {
            return zone switch
            {
                GTZone.cityNoBuildings => "City",
                GTZone.skyJungle => "Clouds",
                GTZone.cityWithSkyJungle => "City",
                GTZone.monkeBlocksShared => "Share My Blocks",
                GTZone.monkeBlocks => "Monke Blocks",
                GTZone.customMaps => "Virtual Stump",
                GTZone.ghostReactor => "Ghost Reactor",
                GTZone.hoverboard => "Hoverpark",
                _ => Enum.GetName(typeof(GTZone), zone).ToTitleCase()
            };
        }
    }
}
