using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal static class ForcedTooltipManager
    {
        //This class is intended to help generate tooltips for servers before tooltips existed.

        private static readonly Dictionary<uint, long> _requestedSingleClick = new();
        private const long CLICK_DEBOUNCE_DELAY = 1500;
        private const uint UPDATE_DELAY = 750;
        public static void RequestName(World world, uint serial)
        {
            if (world.ClientFeatures.TooltipsEnabled) return;

            if (_requestedSingleClick.TryGetValue(serial, out long timeRequested))
                if (Time.Ticks < timeRequested)
                    return;

            if (world.OPL.TryGetRevision(serial, out uint revision) && revision > Time.Ticks) return;

            _requestedSingleClick[serial] = Time.Ticks + CLICK_DEBOUNCE_DELAY;
            GameActions.SingleClick(world, serial);
        }

        public static bool IsObjectTextRequested(World world, Entity parent, string text, ushort hue)
        {
            if (parent == null || world.ClientFeatures.TooltipsEnabled) return false;

            if (!_requestedSingleClick.ContainsKey(parent.Serial)) return false;

            if (Time.Ticks > _requestedSingleClick[parent.Serial])
            {
                _requestedSingleClick.Remove(parent.Serial);
                return false;
            }

            //bool is_probably_mobile = !string.IsNullOrEmpty(name);
            //bool is_probably_mobile_name = is_mobile && !string.IsNullOrEmpty(text) && text.Contains(name);
            bool text_received_within_update_delay = world.OPL.TryGetRevision(parent.Serial, out uint rev) && rev >= Time.Ticks;
            bool has_existing_data = world.OPL.TryGetNameAndData(parent.Serial, out string name, out string data);

            if (text_received_within_update_delay && has_existing_data)
            {
                if (!string.IsNullOrEmpty(name) && !name.Contains(text)) //Item has a name, but it doesn't match what was sent. This could be another line of data.
                {

                    if ((!string.IsNullOrEmpty(data) && data.IndexOf(text) < 0) || string.IsNullOrEmpty(data)) //Annoying IndexOf returns 0 is string is string.Empty
                    {
                        string new_data = string.IsNullOrEmpty(data) ? text : data + "\n" + text;
                        world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, name, new_data, 0);
                    }
                }
                else if (string.IsNullOrEmpty(name)) //Name is empty
                {
                    world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, string.Empty, 0);
                }
            }
            else if (!has_existing_data)
            {
                // assume text is the name since this is initial data
                world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, string.Empty, 0);
            }
            return true;
        }
    }
}
