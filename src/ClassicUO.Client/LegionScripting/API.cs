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

        public uint Found;

        public void SysMsg(string message) => InvokeOnMainThread(() => GameActions.Print(message));
        public PlayerMobile Player() => InvokeOnMainThread(() => World.Player);
        public Item FindItem(uint serial) => InvokeOnMainThread(() => World.Items.Get(serial));
        public bool FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = ushort.MaxValue) =>
            InvokeOnMainThread(() =>
            {
                List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);
                if (result.Count > 0)
                {
                    Found = result[0].Serial;
                    return true;
                }

                return false;
            });

        public Item Backpack() => InvokeOnMainThread(() => World.Player.FindItemByLayer(Game.Data.Layer.Backpack));
    }
}
