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

        public static bool IsObjectTextRequested(World world, Entity parent, string name_from_server, string text, ushort hue)
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
            bool have_name_from_server = !string.IsNullOrWhiteSpace(name_from_server);

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
                    world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, data, 0);
                }
            }
            else if (requested_click && have_text && !have_opl)
            {
                bool is_probably_name =
                    // Items don't have the name set. Mobiles do e.g. name_from_server="Bob" text="Lord Bob (invulnerable)"
                    (parent is not Mobile || have_name_from_server && text.Contains(name_from_server))

                    // Sometimes tags are sent before the actual name e.g. "(tame)" for pets.
                    && char.IsLetterOrDigit(text[0])

                    // Somtimes descriptions containing a colon are sent before the name e.g. "Cost: 500 gold".
                    && !text.Contains(':');

                if (is_probably_name)
                    // text is possibly the name since this is the initial data received after a requested click.
                    world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, text, string.Empty, 0);
                else
                    // If text doesn't seem to be a name, add it to data instead. Name will be set on next message, if no name is set.
                    world.OPL.Add(parent.Serial, Time.Ticks + UPDATE_DELAY, string.Empty, text, 0);
            }
            return requested_click || text_received_within_update_delay;
        }
    }
}
