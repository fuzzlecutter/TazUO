using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClassicUO.Game.Managers
{
    internal static class GraphicsReplacement
    {
        private static Dictionary<ushort, GraphicChangeFilter> mobileChangeFilters = new Dictionary<ushort, GraphicChangeFilter>();
        public static Dictionary<ushort, GraphicChangeFilter> MobileFilters { get { return mobileChangeFilters; } }
        private static HashSet<ushort> quickLookup = new HashSet<ushort>();
        public static void Load()
        {
            if (File.Exists(GetSavePath()))
            {
                try
                {
                    mobileChangeFilters = JsonSerializer.Deserialize<Dictionary<ushort, GraphicChangeFilter>>(File.ReadAllText(GetSavePath()));
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
                    File.WriteAllText(GetSavePath(), JsonSerializer.Serialize<Dictionary<ushort, GraphicChangeFilter>>(mobileChangeFilters));
                }
                catch (Exception e)
                {
                    GameActions.Print($"Failed to save mobile graphic change filter. {e.Message}");
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

        public static void ResetLists()
        {
            Dictionary<ushort, GraphicChangeFilter> newList = new Dictionary<ushort, GraphicChangeFilter>();
            quickLookup.Clear();

            foreach (var item in mobileChangeFilters)
            {
                newList.Add(item.Value.OriginalGraphic, item.Value);
                quickLookup.Add(item.Value.OriginalGraphic);
            }
            mobileChangeFilters = newList;
        }

        public static GraphicChangeFilter NewFilter(ushort originalGraphic, ushort newGraphic, ushort newHue = ushort.MaxValue)
        {
            if (!mobileChangeFilters.ContainsKey(originalGraphic))
            {
                GraphicChangeFilter f;
                mobileChangeFilters.Add(originalGraphic, f = new GraphicChangeFilter()
                {
                    OriginalGraphic = originalGraphic,
                    ReplacementGraphic = newGraphic,
                    NewHue = newHue
                });

                quickLookup.Add(originalGraphic);
                return f;

            }
            return null;
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

    internal class GraphicChangeFilter
    {
        public ushort OriginalGraphic { get; set; }
        public ushort ReplacementGraphic { get; set; }
        public ushort NewHue { get; set; } = ushort.MaxValue;
    }
}