using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class Talk
{
    private static readonly string[] Titles = {
        "Lord ",
        "Lady ",
        "Seer ",
        "Counselor ",
        "Senior Counselor "
        // GMs have a "GM " title, but their actual name includes the title, so that is handled by comparison to name.
    };

    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        Entity entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        var type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        string name = p.ReadASCII(30);
        string text;

        if (p.Length > 44)
        {
            p.Seek(44);
            text = p.ReadASCII();
        }
        else
            text = string.Empty;

        if (
            serial == 0
            && graphic == 0
            && type == MessageType.Regular
            && font == 0xFFFF
            && hue == 0xFFFF
            && name.StartsWith("SYSTEM")
        )
        {
            AsyncNetClient.Socket.Send_ACKTalk();

            return;
        }

        TextType text_type = TextType.SYSTEM;

        if (
            type == MessageType.System
            || serial == 0xFFFF_FFFF
            || serial == 0
            || (name.ToLower() == "system" && entity == null)
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
                entity.Name = string.IsNullOrEmpty(name) ? text : name;

            if (world.ClientFeatures.OldNotorietySystem && entity is Mobile mobile && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text))
            {
                // Best guess that the received text is the mobile's name.
                // Handles prefixes (e.g. "Lord ") and suffixes (e.g. " (frozen)").
                string potential_name = text;
                foreach (string title in Titles)
                {
                    if (potential_name.StartsWith(title))
                    {
                        potential_name = potential_name.Substring(title.Length);
                        break;
                    }
                }

                int suffixIndex = potential_name.LastIndexOf(" (");
                if (suffixIndex > 0 && potential_name.EndsWith(")"))
                    potential_name = potential_name.Substring(0, suffixIndex);

                if (potential_name.Equals(name))
                {
                    mobile.NotorietyFlag = Notoriety.GetOldNotorietyFromHue(hue);
                }
            }
        }

        world.MessageManager.HandleMessage(entity, text, name, hue, type, (byte)font, text_type);
    }
}
