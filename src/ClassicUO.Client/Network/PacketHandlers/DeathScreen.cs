using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class DeathScreen
{
    public static void Receive(World world, ref StackDataReader p)
    {
        // TODO: Prior to client version 1.26.4b, on death, the player was given a grey
        // prompt to continue as a ghost or to insta-res with stat penalties.
        // 1.26.4b and later removed this entirely.
        // action=0: show death dialog prompt
        // action=1: dismiss an existing death dialog prompt (e.g. during GM resurrect)
        byte action = p.ReadUInt8();

        if (action != 1)
        {
            // Capture the final frame before the death screen is drawn over it.
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScreenshotOnDeath){
                Client.Game.TakeScreenshot("death");
                GameActions.Print(TazLang.Get("death_screen_disable"), Constants.HUE_WARN);
            }
            world.Weather.Reset();

            Client.Game.Audio.PlayMusic(Client.Game.Audio.DeathMusicIndex, true);

            if (ProfileManager.CurrentProfile.EnableDeathScreen)
                world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;

            GameActions.RequestWarMode(world.Player, false);
            world.WMapManager._corpse = new WMapEntity(world.Player.Serial)
            {
                X = world.Player.X,
                Y = world.Player.Y,
                HP = 0,
                Map = world.Map.Index,
                LastUpdate = Time.Ticks + 1000 * 60 * 5,
                IsGuild = false,
                Name = "Your Corpse"
            };

            EventSink.InvokeOnPlayerDeath(world.Player, world.Player.Serial);

            if (action == 0 && Client.Game.UO.Version < ClassicUO.Utility.ClientVersion.CV_1264B)
            {
                // TODO: Add a local grey prompt UI to select choice.
                // choice: 1 = insta-resurrect with stat penalties, 2 = stay as ghost
                byte choice = 2;
                AsyncNetClient.Socket.Send_DeathScreen(choice);
            }
        }
    }
}
