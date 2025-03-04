using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;

namespace ClassicUO.Game.UI
{
    internal class NearbyLootGump : Gump
    {
        public const int WIDTH = 300;
        public const int HEIGHT = 700;

        public static int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (value < 0)
                    selectedIndex = 0;
                else
                    selectedIndex = value;
            }
        }

        private ScrollArea scrollArea;
        private DataBox dataBox;
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

            Add(scrollArea = new ScrollArea(0, 0, Width, Height, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            scrollArea.Add(dataBox = new DataBox(0, 0, Width, Height));

            UpdateNearbyLoot();
        }

        public override void SlowUpdate()
        {
            base.SlowUpdate();
            UpdateNearbyLoot();
        }
        private void UpdateNearbyLoot()
        {
            dataBox.Clear();
            itemCount = 0;

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
    }

    internal class NearbyItemDisplay : Control
    {
        private static Queue<NearbyItemDisplay> pool = new Queue<NearbyItemDisplay>();
        private ItemGump itemGump;
        private Label itemLabel;
        private AlphaBlendControl alphaBG;
        private int index;
        private ushort bgHue
        {
            get
            {
                if (NearbyLootGump.SelectedIndex == index)
                {
                    return 53;
                }

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

            Add(alphaBG = new AlphaBlendControl() { Width = Width, Height = Height, Hue = bgHue });

            SetItem(item, index);
        }

        public void SetItem(Item item, int index)
        {
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

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            AutoLootManager.LootItems.Enqueue(itemGump.LocalSerial);
            return base.OnMouseDoubleClick(x, y, button);
        }
    }
}
