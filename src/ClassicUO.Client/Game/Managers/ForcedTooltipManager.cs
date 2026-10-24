using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal static class ForcedTooltipManager
    {
        //This class is intended to help generate tooltips for servers before tooltips existed.

        private static readonly Dictionary<uint, long> _requestedSingleClick = new();
        private const long CLICK_DEBOUNCE_DELAY = 1200;
        private const uint UPDATE_DELAY = 350;
        public static void RequestName(World world, uint serial)
        {
            if (world.ClientFeatures.TooltipsEnabled)
                return;

            if (_requestedSingleClick.TryGetValue(serial, out long timeRequested))
                if (Time.Ticks < timeRequested)
                    return;

            if (world.OPL.TryGetRevision(serial, out uint revision) && revision > Time.Ticks)
                return;

            _requestedSingleClick[serial] = Time.Ticks + CLICK_DEBOUNCE_DELAY;
            GameActions.SingleClick(world, serial);
        }

        public static bool IsObjectTextRequested(World world, Entity parent, string text, ushort hue)
        {
            if (parent == null || world.ClientFeatures.TooltipsEnabled) return false;

            bool requested_click = false;
            if (_requestedSingleClick.TryGetValue(parent.Serial, out long time_requested))
            {
                _requestedSingleClick.Remove(parent.Serial);
                requested_click = Time.Ticks <= time_requested;
            }

            bool text_received_within_update_delay = world.OPL.TryGetRevision(parent.Serial, out uint rev) && rev >= Time.Ticks;
            bool have_opl = world.OPL.TryGetNameAndData(parent.Serial, out string name, out string data);
            bool have_text = !string.IsNullOrWhiteSpace(text);
            bool have_name = !string.IsNullOrWhiteSpace(name);

            if (have_opl && have_text && text_received_within_update_delay)
            {
                if (have_name && !name.Contains(text)) //Item has a name, but it doesn't match what was sent. This could be another line of data.
                {
                    bool have_data = !string.IsNullOrWhiteSpace(data);
                    if (!have_data || !data.Contains(text))
                    {
                        string new_data = have_data ? data + "\n" + text : text;
                        world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, name, new_data, 0);
                    }
                }
                else if (!have_name)
                {
                    world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, string.Empty, 0);
                }
            }
            else if (have_text && !have_opl)
            {
                // assume text is the name since this is initial data received
                world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, string.Empty, 0);
            }
            return requested_click || text_received_within_update_delay;
        }
    }
}
