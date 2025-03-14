using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        #region Properties
        /// <summary>
        /// Used for methods searching for items that don't return the item.
        /// </summary>
        public uint Found { get; private set; }
        public uint LeftHandClearedItem { get; private set; }
        public uint RightHandClearedItem { get; private set; }

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
            Mask = 0x7,
            Running = 0x80,
            NONE = 0xED
        }
        #endregion

        #region Methods
        public void Attack(uint serial) => InvokeOnMainThread(() => GameActions.Attack(serial));
        public bool BandageSelf() => InvokeOnMainThread(GameActions.BandageSelf);
        public void ClearLeftHand() => InvokeOnMainThread(() =>
        {
            Item i = World.Player.FindItemByLayer(Game.Data.Layer.OneHanded);
            if (i != null)
            {
                GameActions.GrabItem(i, i.Amount, Backpack);
                LeftHandClearedItem = i;
            }
        });
        public void ClearRightHand() => InvokeOnMainThread(() =>
        {
            Item i = World.Player.FindItemByLayer(Game.Data.Layer.TwoHanded);
            if (i != null)
            {
                GameActions.GrabItem(i, i.Amount, Backpack);
                RightHandClearedItem = i;
            }
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
        public bool FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread(() =>
            {
                List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);
                if (result.Count > 0 && result[0].Amount >= minamount)
                {
                    Found = result[0].Serial;
                    return true;
                }

                return false;
            });
        public void UseObject(uint serial, bool skipQueue = true) => InvokeOnMainThread(() => { if (skipQueue) GameActions.DoubleClick(serial); else GameActions.DoubleClickQueued(serial); });
        #endregion
    }
}
