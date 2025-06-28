using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellBar : Gump
{
    public static SpellBar Instance { get; private set; }
    
    private SpellEntry[] spellEntries = new SpellEntry[10];
    private int spellRow = 0;
    private TextBox rowLabel;
    
    public SpellBar() : base(0, 0)
    {
        Instance?.Dispose();
        Instance = this;
        CanMove = true;
        CanCloseWithRightClick = false;
        CanCloseWithEsc = false;

        Width = 500;
        Height = 48;
        
        CenterXInViewPort();
        CenterYInViewPort();
        
        Build();
    }

    public override bool AcceptMouseInput { get; set; } = true;

    protected override void OnMouseWheel(MouseEventType delta)
    {
        base.OnMouseWheel(delta);

        if (delta == MouseEventType.WheelScrollDown)
            ChangeRow(true);
        else
            ChangeRow(false);
    }

    private void ChangeRow(bool up)
    {
        if (up)
            spellRow -= 1;
        else
            spellRow += 1;
        
        
        if (spellRow < 0)
            spellRow = SpellBarManager.SpellBarRows.Count - 1;

        if (spellRow >= SpellBarManager.SpellBarRows.Count)
            spellRow = 0;

        rowLabel.SetText(spellRow.ToString());
        
        for (int s = 0; s < spellEntries.Length; s++)
        {
            spellEntries[s].SetSpell(SpellBarManager.SpellBarRows[spellRow].SpellSlot[s], spellRow, s);
        }
    }

    private void Build()
    {
        Add(new AlphaBlendControl() { Width = Width, Height = Height });

        int x = 2;
        
        for (int i = 0; i < spellEntries.Length; i++)
        {
            Add(spellEntries[i] = new SpellEntry().SetSpell(SpellBarManager.SpellBarRows[spellRow].SpellSlot[i], spellRow, i));
            spellEntries[i].X = x;
            spellEntries[i].Y = 1;
            x += 46 + 2;
        }
        
        rowLabel = TextBox.GetOne(spellRow.ToString(), TrueTypeLoader.EMBEDDED_FONT, 12, Color.White, TextBox.RTLOptions.Default());
        rowLabel.X = 482;
        rowLabel.Y = (Height - rowLabel.Height) >> 1;
        Add(rowLabel);
        
        var up = new EmbeddedGumpPic(Width - 16, 0, PNGLoader.Instance.EmbeddedArt["upicon.png"], 148);
        up.MouseUp += (sender, e) => { ChangeRow(false); };
        var down = new EmbeddedGumpPic(Width - 16, Height - 16, PNGLoader.Instance.EmbeddedArt["downicon.png"], 148);
        down.MouseUp += (sender, e) => { ChangeRow(true); };
        
        Add(up);
        Add(down);
    }

    public class SpellEntry : Control
    {
        private GumpPic icon;
        private SpellDefinition spell;
        private int row, col;
        public SpellEntry()
        {
            CanMove = true;
            Width = 46;
            Height = 46;
            Build();
        }
        public SpellEntry SetSpell(SpellDefinition spell, int row, int col)
        {
            this.spell = spell;
            this.row = row;
            this.col = col;
            SpellBarManager.SpellBarRows[row].SpellSlot[col] = spell;
            if(spell != null)
            {
                icon.Graphic = (ushort)spell.GumpIconSmallID;
                icon.IsVisible = true;
            }
            else
            {
                icon.IsVisible = false;
            }

            return this;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if(button == MouseButtonType.Right)
                ContextMenu?.Show();

            if (button == MouseButtonType.Left && spell != null)
            {
                GameActions.CastSpell(spell.ID);
            }
        }

        public override bool AcceptMouseInput { get; set; } = true;

        private void Build()
        {
            Add(new AlphaBlendControl() { Width = 44, Height = 44, X = 1, Y = 1 });
            Add(icon = new GumpPic(1, 1, 0x5000, 0) {IsVisible = false});

            ContextMenu = new();
            ContextMenu.Add(new ContextMenuItemEntry("Set spell", () =>
            {
                UIManager.Add
                (
                    new SpellQuickSearch
                    (
                        ScreenCoordinateX - 20, ScreenCoordinateY - 90, (s) =>
                        {
                            SetSpell(s, row, col);
                        }, true
                    )
                );
            }));
        }
    }
}