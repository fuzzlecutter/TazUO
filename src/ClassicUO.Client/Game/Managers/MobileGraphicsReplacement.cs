using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClassicUO.Game.Managers
{
    internal static class MobileGraphicsReplacement
    {
        private static Dictionary<ushort, MobileChangeFilter> mobileChangeFilters = new Dictionary<ushort, MobileChangeFilter>();
        public static Dictionary<ushort, MobileChangeFilter> MobileFilters { get { return mobileChangeFilters; } }
        private static HashSet<ushort> quickLookup = new HashSet<ushort>();
        public static void Load()
        {
            if (File.Exists(GetSavePath()))
            {
                try
                {
                    mobileChangeFilters = JsonSerializer.Deserialize<Dictionary<ushort, MobileChangeFilter>>(File.ReadAllText(GetSavePath()));
                    foreach (var filter in mobileChangeFilters)
                        quickLookup.Add(filter.Key);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void Save()
        {
            if (mobileChangeFilters.Count > 0)
                try
                {
                    File.WriteAllText(GetSavePath(), JsonSerializer.Serialize<Dictionary<ushort, MobileChangeFilter>>(mobileChangeFilters));
                }
                catch (Exception e)
                {
                    GameActions.Print("Failed to save mobile graphic change filter.");
                }
            mobileChangeFilters.Clear();
            quickLookup.Clear();
        }

        public static void Replace(ref ushort graphic, ref ushort hue)
        {
            if (quickLookup.Contains(graphic))
            {
                var filter = mobileChangeFilters[graphic];
                graphic = filter.ReplacementGraphic;
                if (filter.NewHue != ushort.MaxValue)
                    hue = filter.NewHue;
            }
        }

        public static MobileChangeFilter NewFilter(ushort originalGraphic, ushort newGraphic, ushort newHue = ushort.MaxValue)
        {
            MobileChangeFilter f;
            mobileChangeFilters.Add(originalGraphic, f = new MobileChangeFilter()
            {
                OriginalGraphic = originalGraphic,
                ReplacementGraphic = newGraphic,
                NewHue = newHue
            });

            quickLookup.Add(originalGraphic);

            return f;
        }

        public static void DeleteFilter(ushort originalGraphic)
        {
            if (mobileChangeFilters.ContainsKey(originalGraphic))
                mobileChangeFilters.Remove(originalGraphic);

            if (quickLookup.Contains(originalGraphic))
                quickLookup.Remove(originalGraphic);
        }

        private static string GetSavePath()
        {
            return Path.Combine(CUOEnviroment.ExecutablePath, "Data", "MobileReplacementFilter.json");
        }
    }

    internal class MobileChangeFilter
    {
        public ushort OriginalGraphic { get; set; }
        public ushort ReplacementGraphic { get; set; }
        public ushort NewHue { get; set; } = ushort.MaxValue;
    }
}