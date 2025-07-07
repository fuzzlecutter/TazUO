using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellBar : Gump
{
    public static SpellBar Instance { get; private set; }
    
    private SpellEntry[] spellEntries = new SpellEntry[10];
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
        
        EventSink.SpellCastBegin += EventSinkOnSpellCastBegin;
    }

    private void EventSinkOnSpellCastBegin(object sender, int e)
    {
        foreach (SpellEntry entry in spellEntries)
        {
            if (entry.CurrentSpellID == e)
            {
                entry.BeginTrackingCasting();
            }
        }
    }

    public override GumpType GumpType { get; } = GumpType.SpellBar;

    protected override void OnMouseWheel(MouseEventType delta)
    {
        base.OnMouseWheel(delta);

        if (delta == MouseEventType.WheelScrollDown)
            ChangeRow(true);
        else
            ChangeRow(false);
    }

    public void SetRow(int row)
    {
        SpellBarManager.CurrentRow = row;
        
        if (SpellBarManager.CurrentRow < 0)
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;

        if (SpellBarManager.CurrentRow >= SpellBarManager.SpellBarRows.Count)
            SpellBarManager.CurrentRow = 0;
        
        rowLabel.SetText(SpellBarManager.CurrentRow.ToString());
        
        for (int s = 0; s < spellEntries.Length; s++)
        {
            spellEntries[s].SetSpell(SpellBarManager.GetSpell(SpellBarManager.CurrentRow, s), SpellBarManager.CurrentRow, s);
        }
    }

    public void ChangeRow(bool up)
    {
        if (up)
            SetRow(SpellBarManager.CurrentRow - 1);
        else
            SetRow(SpellBarManager.CurrentRow + 1);
    }

    public void Build()
    {
        Clear();
        
        Add(new AlphaBlendControl() { Width = Width, Height = Height });

        int x = 2;
        
        if(SpellBarManager.CurrentRow > SpellBarManager.SpellBarRows.Count - 1)
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;
        
        for (int i = 0; i < spellEntries.Length; i++)
        {
            Add(spellEntries[i] = new SpellEntry().SetSpell(SpellBarManager.GetSpell(SpellBarManager.CurrentRow, i), SpellBarManager.CurrentRow, i));
            spellEntries[i].X = x;
            spellEntries[i].Y = 1;
            x += 46 + 2;
        }
        
        rowLabel = TextBox.GetOne(SpellBarManager.CurrentRow.ToString(), TrueTypeLoader.EMBEDDED_FONT, 12, Color.White, TextBox.RTLOptions.DefaultCentered(16));
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
    
    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        if (button == MouseButtonType.Left && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
        {
            ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);
            if (texture.Texture != null)
            {
                if (x >= 0 && x <= texture.UV.Width && y >= 0 && y <= texture.UV.Height)
                {
                    IsLocked = !IsLocked;
                }
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        EventSink.SpellCastBegin -= EventSinkOnSpellCastBegin;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y))
            return false;
        
        if (Keyboard.Alt)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);

            if (texture.Texture != null)
            {
                if (IsLocked)
                {
                    hueVector.X = 34;
                    hueVector.Y = 1;
                }
                batcher.Draw
                (
                    texture.Texture,
                    new Vector2(x, y),
                    texture.UV,
                    hueVector
                );
            }
        }

        return true;
    }

    public class SpellEntry : Control
    {
        public int CurrentSpellID => spell?.ID ?? -1;
        
        private GumpPic icon;
        private SpellDefinition spell;
        private int row, col;
        private bool trackCasting;
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
            if(spell != null && spell != SpellDefinition.EmptySpell)
            {
                icon.Graphic = (ushort)spell.GumpIconSmallID;
                icon.IsVisible = true;
                
                int cliloc = GetSpellTooltip(spell.ID);
                
                if (cliloc != 0)
                    SetTooltip(ClilocLoader.Instance.GetString(cliloc), 80);
                else
                    SetTooltip(string.Empty);
            }
            else
            {
                SetTooltip("Right click to set spell");
                icon.IsVisible = false;
            }

            return this;
        }

        /// <summary>
        /// Only call this when you're sure it's being casted.
        /// </summary>
        public void BeginTrackingCasting()
        {
            trackCasting = true;
        }
        public void Cast()
        {
            if (spell != null && spell != SpellDefinition.EmptySpell)
            {
                GameActions.CastSpell(spell.ID);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if(button == MouseButtonType.Right)
                ContextMenu?.Show();

            if (button == MouseButtonType.Left && !Keyboard.Alt && !Keyboard.Ctrl)
            {
                Cast();
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
            ContextMenu.Add(new ContextMenuItemEntry("Clear", () =>
            {
                SetSpell(SpellDefinition.EmptySpell, row, col);
            }));
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
                return false;

            if (trackCasting)
            {
                if (!SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
                {
                    trackCasting = false;

                    return true;
                }
                
                SpellVisualRangeManager.SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();

                if (i == null)
                {
                    trackCasting = false;
                    return true;
                }
                

                if (i.CastTime > 0)
                {
                    double percent = (DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime).TotalSeconds / i.CastTime;
                    if(percent < 0)
                        percent = 0;

                    if (percent > 1)
                        percent = 1;
                    
                    int filledHeight = (int)(Height * percent);
                    int yb = Height - filledHeight; // This shifts the rect up as it grows

                    Rectangle rect = new(x, y + yb, Width, filledHeight);
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.Black), rect, new Vector3(0, 0, 0.65f));
                }
                else
                {
                    trackCasting = false;
                }

            }
            
            return true;
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