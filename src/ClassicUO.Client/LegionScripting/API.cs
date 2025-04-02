using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Python scripting access point
    /// </summary>
    public class API
    {
        public static readonly ConcurrentQueue<Action> QueuedPythonActions = new();
        private static T InvokeOnMainThread<T>(Func<T> func)
        {
            var resultEvent = new ManualResetEvent(false);
            T result = default;

            void action()
            {
                result = func();
                resultEvent.Set();
            }

            QueuedPythonActions.Enqueue(action);
            resultEvent.WaitOne(); // Wait for the main thread to complete the operation
            return result;
        }
        private static void InvokeOnMainThread(Action action)
        {
            var resultEvent = new ManualResetEvent(false);

            void wrappedAction()
            {
                action();
                resultEvent.Set();
            }

            QueuedPythonActions.Enqueue(wrappedAction);
            resultEvent.WaitOne();
        }

        private ConcurrentBag<uint> ignoreList = new();
        private ConcurrentQueue<JournalEntry> journalEntries = new();
        public ConcurrentQueue<JournalEntry> JournalEntries { get { return journalEntries; } }

        #region Properties
        /// <summary>
        /// Get the players backpack
        /// </summary>
        public Item Backpack { get { return InvokeOnMainThread(() => World.Player.FindItemByLayer(Game.Data.Layer.Backpack)); } }
        /// <summary>
        /// Returns the player character
        /// </summary>
        public PlayerMobile Player { get { return InvokeOnMainThread(() => World.Player); } }
        /// <summary>
        /// Can be used for random numbers.
        /// `API.Random.Next(1, 100)` will return a number between 1 and 100.
        /// `API.Random.Next(100)` will return a number between 0 and 100.
        /// </summary>
        public Random Random { get; set; } = new();
        #endregion

        #region Enum
        public enum ScanType
        {
            Hostile = 0,
            Party,
            Followers,
            Objects,
            Mobiles
        }

        public enum Notoriety : byte
        {
            Unknown = 0x00,
            Innocent = 0x01,
            Ally = 0x02,
            Gray = 0x03,
            Criminal = 0x04,
            Enemy = 0x05,
            Murderer = 0x06,
            Invulnerable = 0x07
        }
        #endregion

        #region Methods
        /// <summary>
        /// Attack a mobile
        /// </summary>
        /// <param name="serial"></param>
        public void Attack(uint serial) => InvokeOnMainThread(() => GameActions.Attack(serial));

        /// <summary>
        /// Attempt to bandage yourself. Older clients this will not work, you will need to find a bandage, use it, and target yourself.
        /// </summary>
        /// <returns>True if bandages found and used</returns>
        public bool BandageSelf() => InvokeOnMainThread(GameActions.BandageSelf);

        /// <summary>
        /// If you have an item in your left hand, move it to your backpack
        /// </summary>
        /// <returns>The item that was in your hand</returns>
        public Item ClearLeftHand() => InvokeOnMainThread(() =>
        {
            Item i = World.Player.FindItemByLayer(Game.Data.Layer.OneHanded);
            if (i != null)
            {
                GameActions.GrabItem(i, i.Amount, Backpack);
                return i;
            }
            return null;
        });

        /// <summary>
        /// If you have an item in your right hand, move it to your backpack
        /// </summary>
        /// <returns>The item that was in your hand</returns>
        public Item ClearRightHand() => InvokeOnMainThread(() =>
        {
            Item i = World.Player.FindItemByLayer(Game.Data.Layer.TwoHanded);
            if (i != null)
            {
                GameActions.GrabItem(i, i.Amount, Backpack);
                return i;
            }
            return null;
        });

        /// <summary>
        /// Single click an object
        /// </summary>
        /// <param name="serial"></param>
        public void ClickObject(uint serial) => InvokeOnMainThread(() => GameActions.SingleClick(serial));

        /// <summary>
        /// Attempt to use(double click) an object.
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void UseObject(uint serial, bool skipQueue = true) => InvokeOnMainThread(() => { if (skipQueue) GameActions.DoubleClick(serial); else GameActions.DoubleClickQueued(serial); });

        /// <summary>
        /// Get an item count for the contents of a container
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        public int Contents(uint serial) => InvokeOnMainThread<int>(() =>
        {
            Item i = World.Items.Get(serial);
            if (i != null) return (int)Utility.ContentsCount(i);
            return 0;
        });

        /// <summary>
        /// Send a context menu(right click menu) response.
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="entry"></param>
        public void ContextMenu(uint serial, ushort entry) => InvokeOnMainThread(() =>
        {
            PopupMenuGump.CloseNext = serial;
            NetClient.Socket.Send_RequestPopupMenu(serial);
            NetClient.Socket.Send_PopupMenuSelection(serial, entry);
        });

        /// <summary>
        /// Attempt to equip an item. Layer is automatically detected.
        /// </summary>
        /// <param name="serial"></param>
        public void EquipItem(uint serial) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, 1))
                GameActions.Equip(serial);
        });

        /// <summary>
        /// Move an item to another container
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="destination"></param>
        /// <param name="amt">Amount to move</param>
        /// <param name="x">X coordinate inside a container</param>
        /// <param name="y">Y coordinate inside a container</param>
        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, amt))
                GameActions.DropItem(serial, x, y, 0, destination);
        });

        /// <summary>
        /// Move an item to the ground near you
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="amt">0 to grab entire stack</param>
        /// <param name="x">Offset from your location</param>
        /// <param name="y">Offset from your location</param>
        /// <param name="z">Offset from your location</param>
        public void MoveItemOffset(uint serial, int amt = 0, int x = 0, int y = 0, int z = 0) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, amt))
                GameActions.DropItem(
                    serial,
                    World.Player.X + x,
                    World.Player.Y + y,
                    World.Player.Z + z,
                    0
                );
        });

        /// <summary>
        /// Use a skill
        /// </summary>
        /// <param name="skillName"></param>
        public void UseSkill(string skillName) => InvokeOnMainThread(() =>
        {
            if (skillName.Length > 0)
            {
                for (int i = 0; i < World.Player.Skills.Length; i++)
                {
                    if (World.Player.Skills[i].Name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        GameActions.UseSkill(World.Player.Skills[i].Index);
                        break;
                    }
                }
            }
        });

        /// <summary>
        /// Attempt to cast a spell by its name
        /// </summary>
        /// <param name="spellName">This can be a partial match. Fireba will cast Fireball.</param>
        public void CastSpell(string spellName) => InvokeOnMainThread(() => { GameActions.CastSpellByName(spellName); });

        /// <summary>
        /// Check if a buff is active
        /// </summary>
        /// <param name="buffName">The name/title of the buff</param>
        /// <returns></returns>
        public bool BuffExists(string buffName) => InvokeOnMainThread(() =>
        {
            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
            {
                if (buff.Title.Contains(buffName))
                    return true;
            }

            return false;
        });

        /// <summary>
        /// Show a system message(Left side of screen)
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="hue">Color of the message</param>
        public void SysMsg(string message, ushort hue = 946) => InvokeOnMainThread(() => GameActions.Print(message, hue));

        /// <summary>
        /// Say a message.
        /// </summary>
        /// <param name="message">The message to say</param>
        public void Msg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.SpeechHue);
        });

        /// <summary>
        /// Show a message above a mobile or item, this is only visible to you
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="serial">The item or mobile</param>
        public void HeadMsg(string message, uint serial) => InvokeOnMainThread(() =>
        {
            Entity e = World.Get(serial);
            if (e == null) return;
            MessageManager.HandleMessage(e, message, "", ProfileManager.CurrentProfile.SpeechHue, MessageType.Label, 3, TextType.OBJECT);
        });

        /// <summary>
        /// Send a message to your party
        /// </summary>
        /// <param name="message">The message</param>
        public void PartyMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.SayParty(message);
        });

        /// <summary>
        /// Send your guild a message
        /// </summary>
        /// <param name="message"></param>
        public void GuildMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild);
        });

        /// <summary>
        /// Send a message to your alliance
        /// </summary>
        /// <param name="message"></param>
        public void AllyMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance);
        });

        /// <summary>
        /// Whisper a message
        /// </summary>
        /// <param name="message"></param>
        public void WhisperMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper);
        });

        /// <summary>
        /// Yell a message
        /// </summary>
        /// <param name="message"></param>
        public void YellMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.YellHue, MessageType.Yell);
        });

        /// <summary>
        /// Emote a message
        /// </summary>
        /// <param name="message"></param>
        public void EmoteMsg(string message) => InvokeOnMainThread(() =>
        {
            GameActions.Say(message, ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote);
        });

        /// <summary>
        /// Try to get an item by its serial
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <returns></returns>
        public Item FindItem(uint serial) => InvokeOnMainThread(() => World.Items.Get(serial));

        /// <summary>
        /// Attempt to find an item by type(graphic)
        /// </summary>
        /// <param name="graphic">Graphic/Type of item to find</param>
        /// <param name="container">Container to search</param>
        /// <param name="range">Max range of item(if on ground)</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="minamount">Only match if item stack is at least this much</param>
        /// <returns>Returns the first item found that matches</returns>
        public Item FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread(() =>
            {
                List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);
                foreach (Item i in result)
                {
                    if (i.Amount >= minamount && !ignoreList.Contains(i))
                    {
                        return i;
                    }
                }
                return null;
            });

        /// <summary>
        /// Return a list of items matching the parameters set
        /// </summary>
        /// <param name="graphic">Graphic/Type of item to find</param>
        /// <param name="container">Container to search</param>
        /// <param name="range">Max range of item(if on ground)</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="minamount">Only match if item stack is at lease this much</param>
        /// <returns></returns>
        public Item[] FindTypeAll(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread(() =>
                Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range).Where(i=>!OnIgnoreList(i) && i.Amount >= minamount).ToArray()
            );

        /// <summary>
        /// Attempt to find an item on a layer
        /// </summary>
        /// <param name="layer">The layer to check, see https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs</param>
        /// <param name="serial">Optional, if not set it will check yourself, otherwise it will check the mobile requested</param>
        /// <returns>The item if it exists</returns>
        public Item FindLayer(string layer, uint serial = uint.MaxValue) => InvokeOnMainThread(() =>
        {
            Mobile m = serial == uint.MaxValue ? World.Player : World.Mobiles.Get(serial);
            if (m != null)
            {
                Layer matchedLayer = Utility.GetItemLayer(layer.ToLower());
                Item item = m.FindItemByLayer(matchedLayer);
                if (item != null)
                    return item;
            }
            return null;
        });

        /// <summary>
        /// Get all items in a container
        /// </summary>
        /// <param name="container"></param>
        /// <returns>A list of items in the container</returns>
        public Item[] ItemsInContainer(uint container) => InvokeOnMainThread(() =>
        {
            return Utility.FindItems(parentContainer: container).ToArray();
        });

        /// <summary>
        /// Attempt to use the first item found by graphic(type)
        /// </summary>
        /// <param name="graphic">Graphic/Type</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="container">Parent container</param>
        /// <param name="skipQueue">Defaults to true, set to false to queue the double click</param>
        public void UseType(uint graphic, ushort hue = ushort.MaxValue, uint container = uint.MaxValue, bool skipQueue = true) => InvokeOnMainThread(() =>
        {
            var result = Utility.FindItems(graphic, hue: hue, parentContainer: container);
            foreach (Item i in result)
            {
                if (!ignoreList.Contains(i))
                {
                    if (skipQueue)
                        GameActions.DoubleClick(i);
                    else
                        GameActions.DoubleClickQueued(i);
                    return;
                }
            }
        });

        /// <summary>
        /// Create a cooldown bar
        /// </summary>
        /// <param name="seconds">Duration in seconds for the cooldown bar</param>
        /// <param name="text">Text on the cooldown bar</param>
        /// <param name="hue">Hue to color the cooldown bar</param>
        public void CreateCooldownBar(double seconds, string text, ushort hue) => InvokeOnMainThread(() =>
        {
            Game.Managers.CoolDownBarManager.AddCoolDownBar(TimeSpan.FromSeconds(seconds), text, hue, false);
        });

        /// <summary>
        /// Adds an item or mobile to your ignore list.
        /// </summary>
        /// <param name="serial">The item/mobile serial</param>
        public void IgnoreObject(uint serial) => ignoreList.Add(serial);

        /// <summary>
        /// Clears the ignore list
        /// </summary>
        public void ClearIgnoreList() => ignoreList = new();

        /// <summary>
        /// Check if a serial is on the ignore list
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        public bool OnIgnoreList(uint serial) => ignoreList.Contains(serial);

        /// <summary>
        /// Attempt to pathfind to a location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="distance">Distance away from goal to stop.</param>
        public void Pathfind(int x, int y, int z = int.MinValue, int distance = 0) => InvokeOnMainThread(() =>
        {
            if (z == int.MinValue)
                z = World.Player.Z;
            Pathfinder.WalkTo(x, y, z, distance);
        });

        /// <summary>
        /// Attempt to pathfind to a mobile or item
        /// </summary>
        /// <param name="entity">The mobile or item</param>
        /// <param name="distance">Distance to stop from goal</param>
        public void Pathfind(uint entity, int distance = 0) => InvokeOnMainThread(() =>
        {
            var mob = World.Get(entity);
            if (mob != null)
            {
                if (mob is Mobile)
                    Pathfinder.WalkTo(mob.X, mob.Y, mob.Z, distance);
                else if (mob is Item i && i.OnGround)
                    Pathfinder.WalkTo(i.X, i.Y, i.Z, distance);
            }

        });

        /// <summary>
        /// Check if you are already pathfinding.
        /// </summary>
        /// <returns>true/false</returns>
        public bool Pathfinding() => InvokeOnMainThread(() => Pathfinder.AutoWalking);

        /// <summary>
        /// Cancel pathfinding.
        /// </summary>
        public void CancelPathfinding() => InvokeOnMainThread(Pathfinder.StopAutoWalk);

        /// <summary>
        /// Automatically follow a mobile
        /// </summary>
        /// <param name="mobile">The mobile</param>
        public void AutoFollow(uint mobile) => InvokeOnMainThread(() =>
        {
            ProfileManager.CurrentProfile.FollowingMode = true;
            ProfileManager.CurrentProfile.FollowingTarget = mobile;
        });

        /// <summary>
        /// Cancel auto follow mode
        /// </summary>
        public void CancelAutoFollow() => InvokeOnMainThread(() => ProfileManager.CurrentProfile.FollowingMode = false);

        /// <summary>
        /// Run in a direction
        /// </summary>
        /// <param name="direction">north/northeast/south/west/etc</param>
        public void Run(string direction)
        {
            Direction d = Utility.GetDirection(direction);
            InvokeOnMainThread(() => World.Player.Walk(d, true));
        }

        /// <summary>
        /// Walk in a direction
        /// </summary>
        /// <param name="direction">north/northeast/south/west/etc</param>
        public void Walk(string direction)
        {
            Direction d = Utility.GetDirection(direction);
            InvokeOnMainThread(() => World.Player.Walk(d, false));
        }

        /// <summary>
        /// Turn your character a specific direction
        /// </summary>
        /// <param name="direction">north, northeast, etc</param>
        public void Turn(string direction) => InvokeOnMainThread(() =>
        {
            Direction d = Utility.GetDirection(direction);

            if (d != Direction.NONE && World.Player.Direction != d)
                World.Player.Walk(d, false);
        });

        /// <summary>
        /// Attempt to rename something like a pet
        /// </summary>
        /// <param name="serial">Serial of the mobile to rename</param>
        /// <param name="name">The new name</param>
        public void Rename(uint serial, string name) => InvokeOnMainThread(() => { GameActions.Rename(serial, name); });

        /// <summary>
        /// Attempt to dismount if mounted
        /// </summary>
        /// <returns>Returns the serial of your mount</returns>
        public uint Dismount() => InvokeOnMainThread<uint>(() =>
        {
            Item mount = World.Player.FindItemByLayer(Layer.Mount);
            if (mount != null)
            {
                GameActions.DoubleClick(World.Player);
                return mount.Serial;
            }
            return 0;
        });

        /// <summary>
        /// Attempt to mount(double click) 
        /// </summary>
        /// <param name="serial"></param>
        public void Mount(uint serial) => InvokeOnMainThread(() => { GameActions.DoubleClick(serial); });

        /// <summary>
        /// Wait for a target cursor
        /// </summary>
        /// <param name="targetType">Neutral/Harmful/Beneficial</param>
        /// <param name="timeout">Duration in seconds to wait</param>
        public bool WaitForTarget(string targetType = "Neutral", double timeout = 5)
        {
            //Can't use Time.Ticks due to threading concerns
            var expire = DateTime.UtcNow.AddSeconds(timeout);


            TargetType targetT = TargetType.Neutral;
            switch (targetType)
            {
                case "Harmful":
                    targetT = TargetType.Harmful;
                    break;
                case "Beneficial":
                    targetT = TargetType.Beneficial;
                    break;
            }

            while (!InvokeOnMainThread(() => { return TargetManager.IsTargeting && TargetManager.TargetingType == targetT; }))
            {
                if (DateTime.UtcNow > expire)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Target an item or mobile
        /// </summary>
        /// <param name="serial">Serial of the item/mobile to target</param>
        public void Target(uint serial) => InvokeOnMainThread(() => TargetManager.Target(serial));

        /// <summary>
        /// Target a location. Include graphic if targeting a static.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="graphic"></param>
        public void Target(ushort x, ushort y, short z, ushort graphic = ushort.MaxValue) => InvokeOnMainThread(() =>
        {
            if (graphic == ushort.MaxValue)
            {
                TargetManager.Target(0, x, y, z);
            }
            else
            {
                TargetManager.Target(graphic, x, y, z);
            }
        });

        /// <summary>
        /// Request the player to target something
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public uint RequestTarget(double timeout = 5)
        {
            var expire = DateTime.Now.AddSeconds(timeout);
            InvokeOnMainThread(() => TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral));
            while (DateTime.Now < expire)
                if (!InvokeOnMainThread(() => TargetManager.IsTargeting))
                    return TargetManager.LastTargetInfo.Serial;
            InvokeOnMainThread(() => TargetManager.Reset());
            return 0;
        }

        /// <summary>
        /// Target yourself
        /// </summary>
        public void TargetSelf() => InvokeOnMainThread(() => TargetManager.Target(World.Player.Serial));

        /// <summary>
        /// Target a land tile
        /// </summary>
        /// <param name="xOffset">X from your position</param>
        /// <param name="yOffset">Y from your position</param>
        public void TargetLandRel(int xOffset, int yOffset) => InvokeOnMainThread(() =>
        {
            if (!TargetManager.IsTargeting)
                return;

            ushort x = (ushort)(World.Player.X + xOffset);
            ushort y = (ushort)(World.Player.Y + yOffset);

            World.Map.GetMapZ(x, y, out sbyte gZ, out sbyte sZ);
            TargetManager.Target(0, x, y, gZ);
        });

        /// <summary>
        /// Target a tile relative to your location
        /// </summary>
        /// <param name="xOffset">X Offset from your position</param>
        /// <param name="yOffset">Y Offset from your position</param>
        /// <param name="graphic">Optional graphic, will only target if tile matches this</param>
        public void TargetTileRel(int xOffset, int yOffset, uint graphic = uint.MaxValue) => InvokeOnMainThread(() =>
        {
            if (!TargetManager.IsTargeting)
                return;

            ushort x = (ushort)(World.Player.X + xOffset);
            ushort y = (ushort)(World.Player.Y + yOffset);

            GameObject g = World.Map.GetTile(x, y);

            if (graphic != uint.MaxValue && g.Graphic != graphic)
                return;

            TargetManager.Target(g.Graphic, x, y, g.Z);
        });

        /// <summary>
        /// Cancel targeting
        /// </summary>
        public void CancelTarget() => InvokeOnMainThread(TargetManager.CancelTarget);

        /// <summary>
        /// Set a skills lock status
        /// </summary>
        /// <param name="skill">The skill name, can be partia;</param>
        /// <param name="up_down_locked">up/down/locked</param>
        public void SetSkillLock(string skill, string up_down_locked) => InvokeOnMainThread(() =>
        {
            skill = skill.ToLower();
            Lock status = Lock.Up;
            switch (up_down_locked)
            {
                case "down":
                    status = Lock.Down;
                    break;
                case "locked":
                    status = Lock.Locked;
                    break;
            }

            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                {
                    World.Player.Skills[i].Lock = status;
                    break;
                }
            }
        });

        /// <summary>
        /// Logout of the game
        /// </summary>
        public void Logout() => InvokeOnMainThread(() => GameActions.Logout());

        /// <summary>
        /// Gets item name and properties
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="wait">True or false to wait for name and props</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <returns>Item name and properties, or empty if we don't have them.</returns>
        public string ItemNameAndProps(uint serial, bool wait = false, int timeout = 10)
        {
            if (wait)
            {
                var expire = DateTime.UtcNow.AddSeconds(timeout);

                while (!InvokeOnMainThread(() => World.OPL.Contains(serial)) && DateTime.UtcNow < expire)
                {
                    Thread.Sleep(100);
                }
            }

            return InvokeOnMainThread(() =>
            {
                if (World.OPL.TryGetNameAndData(serial, out string n, out string d))
                {
                    return n + "\n" + d;
                }
                return string.Empty;
            });
        }

        /// <summary>
        /// Check if a player has a server gump
        /// </summary>
        /// <param name="ID">Skip to check if player has any gump from server.</param>
        /// <returns>Returns gump id if found</returns>
        public uint HasGump(uint ID = uint.MaxValue) => InvokeOnMainThread<uint>(() =>
        {
            if (World.Player.HasGump && (World.Player.LastGumpID == ID || ID == uint.MaxValue))
            {
                return World.Player.LastGumpID;
            }
            return 0;
        });

        /// <summary>
        /// Reply to a gump
        /// </summary>
        /// <param name="button">Button ID</param>
        /// <param name="gump">Gump ID, leave blank to reply to last gump</param>
        /// <returns>True if gump was found, false if not</returns>
        public bool ReplyGump(int button, uint gump = uint.MaxValue) => InvokeOnMainThread(() =>
        {
            Gump g = UIManager.GetGumpServer(gump == uint.MaxValue ? World.Player.LastGumpID : gump);
            if (g != null)
            {
                GameActions.ReplyGump(g.LocalSerial, g.ServerSerial, button, new uint[0] { }, new Tuple<ushort, string>[0]);
                g.Dispose();
                return true;
            }
            return false;
        });

        /// <summary>
        /// Close the last gump open, or a specific gump
        /// </summary>
        /// <param name="ID">Gump ID</param>
        public void CloseGump(uint ID = uint.MaxValue) => InvokeOnMainThread(() =>
        {
            uint gump = ID != uint.MaxValue ? ID : World.Player.LastGumpID;
            UIManager.GetGumpServer(gump)?.Dispose();
        });

        /// <summary>
        /// Check if a gump contains a specific text.
        /// </summary>
        /// <param name="text">Can be regex if you start with $, otherwise it's just regular search. Case Sensitive.</param>
        /// <param name="ID">Gump ID, blank to use the last gump.</param>
        /// <returns></returns>
        public bool GumpContains(string text, uint ID = uint.MaxValue) => InvokeOnMainThread(() =>
        {
            Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);
            if (g != null)
            {
                bool regex = text.StartsWith("$");
                if (regex)
                    text = text.Substring(1);

                foreach (Control c in g.Children)
                {
                    if (c is Label l && (l.Text.Contains(text) || (regex && System.Text.RegularExpressions.Regex.IsMatch(l.Text, text))))
                    {
                        return true;
                    }
                    else if (c is HtmlControl ht && (ht.Text.Contains(text) || (regex && System.Text.RegularExpressions.Regex.IsMatch(ht.Text, text))))
                    {
                        return true;
                    }
                }
            }

            return false;
        });

        /// <summary>
        /// Toggle flying if you are a gargoyle
        /// </summary>
        public void ToggleFly() => InvokeOnMainThread(() =>
        {
            if (World.Player.Race == RaceType.GARGOYLE)
                NetClient.Socket.Send_ToggleGargoyleFlying();
        });

        /// <summary>
        /// Toggle an ability
        /// </summary>
        /// <param name="ability">primary/secondary/stun/disarm</param>
        public void ToggleAbility(string ability) =>
            InvokeOnMainThread(() =>
            {
                switch (ability.ToLower())
                {
                    case "primary":
                        GameActions.UsePrimaryAbility();
                        break;

                    case "secondary":
                        GameActions.UseSecondaryAbility();
                        break;

                    case "stun":
                        NetClient.Socket.Send_StunRequest();
                        break;

                    case "disarm":
                        NetClient.Socket.Send_DisarmRequest();
                        break;
                }
            });

        /// <summary>
        /// Check if your primary ability is active
        /// </summary>
        /// <returns>true/false</returns>
        public bool PrimaryAbilityActive() => ((byte)World.Player.PrimaryAbility & 0x80) != 0;

        /// <summary>
        /// Check if your secondary ability is active
        /// </summary>
        /// <returns>true/false</returns>
        public bool SecondaryAbilityActive() => ((byte)World.Player.SecondaryAbility & 0x80) != 0;

        /// <summary>
        /// Check if your journal contains a message
        /// </summary>
        /// <param name="msg">The message to check for</param>
        /// <returns>True if message was found</returns>
        public bool InJournal(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return false;

            foreach (var je in JournalEntries.ToArray())
            {
                if (je.Text.Contains(msg)) return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the journal contains *any* of the strings in this list
        /// </summary>
        /// <param name="msgs"></param>
        /// <returns></returns>
        public bool InJournalAny(string[] msgs)
        {
            if(msgs == null || msgs.Length == 0) return false;

            foreach (var je in JournalEntries.ToArray())
            {
                foreach (var msg in msgs)
                    if (je.Text.Contains(msg)) return true;
            }

            return false;
        }

        /// <summary>
        /// Clear your journal(This is specific for each script)
        /// </summary>
        public void ClearJournal()
        {
            while (JournalEntries.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Pause the script
        /// </summary>
        /// <param name="seconds"></param>
        public void Pause(double seconds)
        {
            Thread.Sleep((int)(seconds * 1000));
        }

        /// <summary>
        /// Stops the current script
        /// </summary>
        public void Stop()
        {
            int t = Thread.CurrentThread.ManagedThreadId;
            InvokeOnMainThread(() =>
            {
                if (LegionScripting.PyThreads.TryGetValue(t, out var s))
                    LegionScripting.StopScript(s);
            });
        }

        /// <summary>
        /// Toggle autolooting on or off
        /// </summary>
        public void ToggleAutoLoot() => InvokeOnMainThread(() =>
        {
            ProfileManager.CurrentProfile.EnableAutoLoot ^= true;
        });

        /// <summary>
        /// Use a virtue
        /// </summary>
        /// <param name="virtue">honor/sacrifice/valor</param>
        public void Virtue(string virtue)
        {
            switch (virtue.ToLower())
            {
                case "honor":
                    InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x01); });
                    break;
                case "sacrifice":
                    InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x02); });
                    break;
                case "valor":
                    InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x03); });
                    break;
            }
        }

        /// <summary>
        /// Find the nearest item/mobile based on scan type
        /// </summary>
        /// <param name="scanType"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public Entity NearestEntity(ScanType scanType, int maxDistance = 10) => InvokeOnMainThread(() =>
        {
            uint m = Utility.FindNearestCheckPythonIgnore((ScanTypeObject)scanType, this);

            var e = World.Get(m);

            if (e != null && e.Distance <= maxDistance)
                return e;

            return null;
        });

        /// <summary>
        /// Get the nearest mobile by Notoriety
        /// </summary>
        /// <param name="notoriety">List of notorieties</param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public Mobile NearestMobile(Notoriety[] notoriety, int maxDistance = 10) => InvokeOnMainThread(() =>
        {
            if(notoriety == null || notoriety.Length == 0) return null;

            return World.Mobiles.Values.Where(m => !m.IsDestroyed
                && !m.IsDead
                && notoriety.Contains((Notoriety)(byte)m.NotorietyFlag)
                && m.Distance <= maxDistance
                && !OnIgnoreList(m)).OrderBy(m => m.Distance).FirstOrDefault();
        });

        /// <summary>
        /// Get the nearest corpse within a distance
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Item NearestCorpse(int distance = 3) => InvokeOnMainThread(() =>
            Utility.FindNearestCorpsePython(distance, this)
        );

        /// <summary>
        /// Get a mobile from its serial
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>The mobile or null</returns>
        public Mobile FindMobile(uint serial) => InvokeOnMainThread(() => World.Mobiles.Get(serial));

        /// <summary>
        /// Return a list of all mobiles the client is aware of.
        /// </summary>
        /// <returns></returns>
        public Mobile[] GetAllMobiles() => InvokeOnMainThread(() => { return World.Mobiles.Values.ToArray(); });

        /// <summary>
        /// Get the tile at a location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>A GameObject of that location.</returns>
        public GameObject GetTile(int x, int y) => InvokeOnMainThread(() =>
        {
            return World.Map.GetTile(x, y);
        });

        /// <summary>
        /// Get a blank gump
        /// </summary>
        /// <param name="acceptMouseInput">Allow clicking the gump</param>
        /// <param name="canMove">Allow the play to move this gump</param>
        /// <returns>A new, empty gump</returns>
        public Gump CreateGump(bool acceptMouseInput = true, bool canMove = true)
        {
            var g = new Gump(0, 0)
            {
                AcceptMouseInput = acceptMouseInput,
                CanMove = canMove,
                WantUpdateSize = true
            };
            return g;
        }

        /// <summary>
        /// Add a gump to the players screen
        /// </summary>
        /// <param name="g">The gump to add</param>
        public void AddGump(Gump g) => InvokeOnMainThread(() =>
        {
            UIManager.Add(g);
        });

        /// <summary>
        /// Create a checkbox for gumps
        /// </summary>
        /// <param name="text">Optional text label</param>
        /// <param name="hue">Optional hue</param>
        /// <returns>The checkbox</returns>
        public Checkbox CreateGumpCheckbox(string text = "", ushort hue = 0) => new Checkbox(0x00D2, 0x00D3, text, color: hue) { CanMove = true };

        /// <summary>
        /// Create a label for a gump
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="hue">The hue of the text</param>
        /// <returns></returns>
        public Label CreateGumpLabel(string text, ushort hue = 996) => new Label(text, true, hue) { CanMove = true };

        /// <summary>
        /// Get a transparent color box for gumps
        /// </summary>
        /// <param name="opacity">0.5 = 50%</param>
        /// <param name="color">Html color code like #000000</param>
        /// <returns></returns>
        public AlphaBlendControl CreateGumpColorBox(float opacity = 0.7f, string color = "#000000")
        {
            AlphaBlendControl bc = new AlphaBlendControl(opacity);

            if (color.StartsWith("#") && color.Length == 7)
            {
                byte r = Convert.ToByte(color.Substring(1, 2), 16);
                byte g = Convert.ToByte(color.Substring(3, 2), 16);
                byte b = Convert.ToByte(color.Substring(5, 2), 16);

                bc.BaseColor = new Color(r, g, b);
            }
            else
            {
                bc.BaseColor = Color.Black;
            }
            return bc;
        }

        /// <summary>
        /// Create a picture of an item
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ResizableStaticPic CreateGumpItemPic(uint graphic, int width, int height)
        {
            ResizableStaticPic pic = new ResizableStaticPic(graphic, width, height)
            {
                AcceptMouseInput = false
            };
            return pic;
        }

        /// <summary>
        /// Create a button for gumps
        /// </summary>
        /// <param name="text"></param>
        /// <param name="normal">Graphic when not clicked or hovering</param>
        /// <param name="pressed">Graphic when pressed</param>
        /// <param name="hover">Graphic on hover</param>
        /// <returns></returns>
        public Button CreateGumpButton(string text = "", ushort hue = 996, ushort normal = 0x00EF, ushort pressed = 0x00F0, ushort hover = 0x00EE)
        {
            Button b = new Button(0, normal, pressed, hover, caption: text, normalHue: hue, hoverHue: hue);
            return b;
        }

        /// <summary>
        /// Create a radio button for gumps, use group numbers to only allow one item to be checked at a time
        /// </summary>
        /// <param name="text">Optional text</param>
        /// <param name="group">Group ID</param>
        /// <param name="inactive">Unchecked graphic</param>
        /// <param name="active">Checked graphic</param>
        /// <param name="hue">Text color</param>
        /// <returns></returns>
        public RadioButton CreateGumpRadioButton(string text = "", int group = 0, ushort inactive = 0x00D0, ushort active = 0x00D1, ushort hue = 0xFFFF)
        {
            RadioButton rb = new RadioButton(group, inactive, active, text, color: hue);
            return rb;
        }

        /// <summary>
        /// Get a skill from the player. See the Skill class for what properties are available: https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs
        /// </summary>
        /// <param name="skill">Skill name, case sensitive</param>
        /// <returns></returns>
        public Skill GetSkill(string skill) => InvokeOnMainThread(() =>
        {
            if(string.IsNullOrEmpty(skill)) return null;
            
            foreach (Skill s in World.Player.Skills)
            {
                if (s.Name.Contains(skill))
                    return s;
            }
            return null;
        });
        #endregion
    }
}
