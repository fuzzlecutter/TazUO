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
        AcceptMouseInput = true;

        Width = 500;
        Height = 48;
        
        CenterXInViewPort();
        CenterYInViewPort();
        
        Build();
    }
    
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
            spellEntries[s].SetSpell(SpellBarManager.GetSpell(spellRow, s), spellRow, s);
        }
    }

    private void Build()
    {
        Add(new AlphaBlendControl() { Width = Width, Height = Height });

        int x = 2;
        
        for (int i = 0; i < spellEntries.Length; i++)
        {
            Add(spellEntries[i] = new SpellEntry().SetSpell(SpellBarManager.GetSpell(spellRow, i), spellRow, i));
            spellEntries[i].X = x;
            spellEntries[i].Y = 1;
            x += 46 + 2;
        }
        
        rowLabel = TextBox.GetOne(spellRow.ToString(), TrueTypeLoader.EMBEDDED_FONT, 12, Color.White, TextBox.RTLOptions.DefaultCentered(16));
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
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = true;
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
                
                int cliloc = GetSpellTooltip(spell.ID);
                
                GameActions.Print($"CLILOC: {cliloc}");

                if (cliloc != 0)
                {
                    SetTooltip(ClilocLoader.Instance.GetString(cliloc), 80);
                }
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

            if (button == MouseButtonType.Left && spell != null && !Keyboard.Alt && !Keyboard.Ctrl)
            {
                GameActions.CastSpell(spell.ID);
            }
        }
        
        private void Build()
        {
            Add(new AlphaBlendControl() { Width = 44, Height = 44, X = 1, Y = 1 });
            Add(icon = new GumpPic(1, 1, 0x5000, 0) {IsVisible = false, AcceptMouseInput = false});

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
        
        private static int GetSpellTooltip(int id)
        {
            if (id >= 1 && id <= 64) // Magery
            {
                return 3002011 + (id - 1);
            }

            if (id >= 101 && id <= 117) // necro
            {
                return 1060509 + (id - 101);
            }

            if (id >= 201 && id <= 210)
            {
                return 1060585 + (id - 201);
            }

            if (id >= 401 && id <= 406)
            {
                return 1060595 + (id - 401);
            }

            if (id >= 501 && id <= 508)
            {
                return 1060610 + (id - 501);
            }

            if (id >= 601 && id <= 616)
            {
                return 1071026 + (id - 601);
            }

            if (id >= 678 && id <= 693)
            {
                return 1031678 + (id - 678);
            }

            if (id >= 701 && id <= 745)
            {
                if (id <= 706)
                {
                    return 1115612 + (id - 701);
                }

                if (id <= 745)
                {
                    return 1155896 + (id - 707);
                }
            }

            return 0;
        }
    }
}