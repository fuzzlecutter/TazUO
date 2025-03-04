using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using SDL2;

namespace ClassicUO.Game.UI
{
    internal class NearbyLootGump : Gump
    {
        public const int WIDTH = 250;
        public const int HEIGHT = 550;

        public static int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (value < -1)
                    selectedIndex = -1;
                else
                    selectedIndex = value;
            }
        }

        private ScrollArea scrollArea;
        private DataBox dataBox;
        private NiceButton lootButton;
        private int itemCount = 0;

        private static HashSet<uint> _corpsesRequested = new HashSet<uint>();
        private static int selectedIndex;

        public NearbyLootGump() : base(0, 0)
        {
            UIManager.GetGump<NearbyLootGump>()?.Dispose();

            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            CanCloseWithRightClick = true;
            Width = WIDTH;
            Height = HEIGHT;

            Add(new AlphaBlendControl() { Width = Width, Height = Height });

            Control c;
            Add(c = new TextBox("Nearby corpse loot", Assets.TrueTypeLoader.EMBEDDED_FONT, 24, WIDTH, Color.OrangeRed, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { AcceptMouseInput = false });

            Add(lootButton = new NiceButton(0, c.Height, WIDTH >> 1, 20, ButtonAction.Default, "Loot All"));
            lootButton.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    foreach (Control control in dataBox.Children)
                    {
                        if (control is NearbyItemDisplay display)
                        {
                            AutoLootManager.Instance.LootItem(display.LocalSerial);
                        }
                    }
                }
            };

            Add(c = new NiceButton(WIDTH >> 1, c.Height, WIDTH >> 1, 20, ButtonAction.Default, "Set Loot Bag"));
            c.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;

                GameActions.Print(Resources.ResGumps.TargetContainerToGrabItemsInto);
                TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            };

            Add(scrollArea = new ScrollArea(0, lootButton.Y + lootButton.Height, Width, Height - lootButton.Y - lootButton.Height, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            scrollArea.Add(dataBox = new DataBox(0, 0, Width, scrollArea.Height));

            UpdateNearbyLoot();
        }
        private void UpdateNearbyLoot()
        {
            itemCount = 0;

            ClearDataBox();

            foreach (Item item in World.Items.Values)
            {
                if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    ProcessCorpse(item);
                }
            }

            dataBox.ReArrangeChildren(1);
            dataBox.ForceSizeUpdate(false);

            if (SelectedIndex >= itemCount)
                SelectedIndex = itemCount - 1;
        }
        private void ProcessCorpse(Item corpse)
        {
            if (corpse == null)
                return;

            if (corpse.Items != null)
            {
                for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                {

                    Item item = (Item)i;
                    if (item == null)
                        continue;

                    dataBox.Add(NearbyItemDisplay.GetOne(item, itemCount));
                    itemCount++;
                }

            }
            else
            {
                if (_corpsesRequested.Contains(corpse))
                    return;

                GameActions.DoubleClickQueued(corpse.Serial);

                if (World.Player.AutoOpenedCorpses.Contains(corpse.Serial))
                    return;

                _corpsesRequested.Add(corpse.Serial);
            }
        }
        private void LootSelectedIndex()
        {
            if (SelectedIndex == -1)
                lootButton.InvokeMouseUp(lootButton.Location, MouseButtonType.Left);
            else if (dataBox.Children.Count > SelectedIndex)
            {
                AutoLootManager.Instance.LootItem(dataBox.Children[SelectedIndex].LocalSerial);
            }
        }

        private void ClearDataBox()
        {
            List<Control> removeAfter = new List<Control>();

            foreach (Control c in dataBox.Children)
            {
                if (c is NearbyItemDisplay cd)
                {
                    cd.ReturnToPool();
                    removeAfter.Add(c);
                }
                else
                    c.Dispose();
            }

            foreach (Control c in removeAfter)
                dataBox.Remove(c);
        }

        public static bool IsCorpseRequested(uint serial, bool remove = true)
        {
            if (_corpsesRequested.Contains(serial) && !World.Player.AutoOpenedCorpses.Contains(serial))
            {
                if (remove) _corpsesRequested.Remove(serial);
                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            _corpsesRequested.Clear();
        }
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            base.OnKeyDown(key, mod);

            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_UP:
                    SelectedIndex--;
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN:
                    SelectedIndex++;
                    break;
                case SDL.SDL_Keycode.SDLK_RETURN:
                    LootSelectedIndex();
                    break;

            }
        }
        protected override void OnControllerButtonDown(SDL.SDL_GameControllerButton button)
        {
            base.OnControllerButtonDown(button);
            switch (button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    SelectedIndex--;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    SelectedIndex++;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    LootSelectedIndex();
                    break;
            }
        }
        public override void Update()
        {
            base.Update();

            if (selectedIndex == -1)
                lootButton.IsSelected = true;
            else
                lootButton.IsSelected = false;
        }
        public override void SlowUpdate()
        {
            base.SlowUpdate();
            UpdateNearbyLoot();
        }

    }

    internal class NearbyItemDisplay : Control
    {
        private static Queue<NearbyItemDisplay> pool = new Queue<NearbyItemDisplay>();
        private ItemGump itemGump;
        private Label itemLabel;
        private AlphaBlendControl alphaBG;
        private Item currentItem;
        private int index;
        private ushort bgHue
        {
            get
            {
                if (AutoLootManager.Instance.IsBeingLooted(LocalSerial))
                    return 32;

                if (NearbyLootGump.SelectedIndex == index)
                    return 53;

                return 0;
            }
        }
        public static NearbyItemDisplay GetOne(Item item, int index)
        {
            NearbyItemDisplay nearbyItemDisplay = pool.Count > 0 ? pool.Dequeue() : new NearbyItemDisplay(item, index);

            if (nearbyItemDisplay.IsDisposed)
                return new NearbyItemDisplay(item, index);

            nearbyItemDisplay.SetItem(item, index);
            return nearbyItemDisplay;
        }

        public NearbyItemDisplay(Item item, int index)
        {
            if (item == null)
            {
                Dispose();
                return;
            }

            CanMove = false;
            AcceptMouseInput = true;
            Width = NearbyLootGump.WIDTH;
            this.index = index;

            LocalSerial = item.Serial;

            Add(alphaBG = new AlphaBlendControl() { Width = Width, Height = Height, Hue = bgHue });

            SetItem(item, index);
        }

        public void SetItem(Item item, int index)
        {
            this.currentItem = item;
            if (item == null) return;

            LocalSerial = item.Serial;

            alphaBG.Hue = bgHue; //Prevent weird flashing

            if (itemGump == null)
            {
                Add(itemGump = new ItemGump(item.Serial, item.DisplayedGraphic, item.Hue, 0, 0));
            }
            else
            {
                itemGump.LocalSerial = item.Serial;
                itemGump.Graphic = item.DisplayedGraphic;
                itemGump.Hue = item.Hue;
                itemGump.SetTooltip(item);
            }

            if (itemLabel == null)
            {
                Add(itemLabel = new Label(item.Name, true, 43) { X = 50 });
            }
            else
            {
                itemLabel.Text = item.Name;
            }

            Height = alphaBG.Height = itemGump.Height;

            SetTooltip(item);
            this.index = index;
        }

        public void ReturnToPool()
        {
            pool.Enqueue(this);
        }

        public override void Update()
        {
            base.Update();
            if (alphaBG.Hue != bgHue)
                alphaBG.Hue = bgHue;
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);
            NearbyLootGump.SelectedIndex = index;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (button != MouseButtonType.Left) return;

            if (Keyboard.Shift && currentItem != null && ProfileManager.CurrentProfile.EnableAutoLoot && !ProfileManager.CurrentProfile.HoldShiftForContext && !ProfileManager.CurrentProfile.HoldShiftToSplitStack)
            {
                AutoLootManager.Instance.AddAutoLootEntry(currentItem.Graphic, currentItem.Hue, currentItem.Name);
                GameActions.Print($"Added this item to auto loot.");
            }

            AutoLootManager.Instance.LootItem(LocalSerial);
        }
    }
}
