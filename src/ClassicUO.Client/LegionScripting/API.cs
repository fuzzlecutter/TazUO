using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;

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
        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => InvokeOnMainThread(() =>
        {
            if (GameActions.PickUp(serial, 0, 0, amt))
                GameActions.DropItem(serial, x, y, 0, destination);
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
