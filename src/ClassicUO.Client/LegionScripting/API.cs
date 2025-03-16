using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
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
        public enum Direction : byte
        {
            North = 0x00,
            Right = 0x01,
            East = 0x02,
            Down = 0x03,
            South = 0x04,
            Left = 0x05,
            West = 0x06,
            Up = 0x07,
            Running = 0x80,
            NONE = 0xED
        }
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
        public bool BuffExists(string buffName) => InvokeOnMainThread(() =>
        {
            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
            {
                if (buff.Title.Contains(buffName))
                    return true;
            }

            return false;
        });
        public void SysMsg(string message, ushort hue = 946) => InvokeOnMainThread(() => GameActions.Print(message, hue));
        public Item FindItem(uint serial) => InvokeOnMainThread(() => World.Items.Get(serial));
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
        #endregion
    }
}
