using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.LegionScripting
{
    public class API
    {
        public readonly ConcurrentQueue<Action> QueuedPythonActions = new();
        private T InvokeOnMainThread<T>(Func<T> func)
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
        private void InvokeOnMainThread(Action action)
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

        private ConcurrentDictionary<uint, byte> ignoreList = new ConcurrentDictionary<uint, byte>();

        #region Properties
        /// <summary>
        /// Get the players backpack
        /// </summary>
        public Item Backpack { get { return InvokeOnMainThread(() => World.Player.FindItemByLayer(Game.Data.Layer.Backpack)); } }
        public PlayerMobile Player { get { return InvokeOnMainThread(() => World.Player); } }
        #endregion

        #region Methods
        public void Attack(uint serial) => InvokeOnMainThread(() => GameActions.Attack(serial));
        public bool BandageSelf() => InvokeOnMainThread(GameActions.BandageSelf);
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
        public void ClickObject(uint serial) => InvokeOnMainThread(() => GameActions.SingleClick(serial));
        public int Contents(uint serial) => InvokeOnMainThread(() =>
        {
            Item i = World.Items.Get(serial);
            if (i != null) return i.Amount;
            return 0;
        });
        public void ContextMenu(uint serial, ushort entry) => InvokeOnMainThread(() =>
        {
            PopupMenuGump.CloseNext = serial;
            NetClient.Socket.Send_RequestPopupMenu(serial);
            NetClient.Socket.Send_PopupMenuSelection(serial, entry);
        });
        public void EquipItem(uint serial) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, 1))
                GameActions.Equip(serial);
        });
        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, amt))
                GameActions.DropItem(serial, x, y, 0, destination);
        });
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
        /// <param name="minamount">Only match if item stack is at lease this much</param>
        /// <returns>Returns the first item found that matches</returns>
        public Item FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread(() =>
            {
                List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);
                foreach (Item i in result)
                {
                    if (i.Amount >= minamount && !ignoreList.ContainsKey(i))
                    {
                        return i;
                    }
                }
                return null;
            });

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
        /// Attempt to use(double click) an object.
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void UseObject(uint serial, bool skipQueue = true) => InvokeOnMainThread(() => { if (skipQueue) GameActions.DoubleClick(serial); else GameActions.DoubleClickQueued(serial); });

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
        public void IgnoreObject(uint serial) => ignoreList.TryAdd(serial, 0);

        /// <summary>
        /// Clears the ignore list
        /// </summary>
        public void ClearIgnoreList() => ignoreList.Clear();

        /// <summary>
        /// Attempt to pathfind to a location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="distance">Distance away from goal to stop.</param>
        public void Pathfind(int x, int y, int z, int distance = 0) => InvokeOnMainThread(() =>
        {
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
        /// Automatically follow a mobile
        /// </summary>
        /// <param name="mobile">The mobile</param>
        public void AutoFollow(uint mobile) => InvokeOnMainThread(() =>
        {
            ProfileManager.CurrentProfile.FollowingMode = true;
            ProfileManager.CurrentProfile.FollowingTarget = mobile;
        });

        /// <summary>
        /// Cancel pathfinding.
        /// </summary>
        public void CancelPathfinding() => InvokeOnMainThread(Pathfinder.StopAutoWalk);

        /// <summary>
        /// Cancel auto follow mode
        /// </summary>
        public void CancelAutoFollow() => InvokeOnMainThread(() => ProfileManager.CurrentProfile.FollowingMode = false);

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
                if (!ignoreList.ContainsKey(i))
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
        /// Target yourself
        /// </summary>
        public void TargetSelf() => InvokeOnMainThread(() => InvokeOnMainThread(() => TargetManager.Target(World.Player.Serial)));

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
        public void Logout() => InvokeOnMainThread(()=>GameActions.Logout());
        #endregion
    }
}
