using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI
{
    internal class NearbyLootGump : Gump
    {
        private ScrollArea scrollArea;
        private DataBox dataBox;
        public NearbyLootGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Width = 300;
            Height = 700;

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

            foreach (Item item in World.Items.Values)
            {
                if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    ProcessCorpse(item);
                }
            }

            dataBox.ReArrangeChildren(5);
            dataBox.ForceSizeUpdate(false);
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

                    dataBox.Add(new Label(item.Name, true, 16));
                }

            }
        }
    }
}
