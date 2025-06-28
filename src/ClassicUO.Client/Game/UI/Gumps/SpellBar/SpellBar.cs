using ClassicUO.Assets;
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

        if (delta == MouseEventType.Up)
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
            spellEntries[s].SetSpell((ushort)SpellBarManager.SpellBarRows[spellRow].SpellSlot[s].GumpIconSmallID);
        }
    }

    private void Build()
    {
        Add(new AlphaBlendControl() { Width = Width, Height = Height });

        int x = 2;
        
        for (int i = 0; i < spellEntries.Length; i++)
        {
            Add(spellEntries[i] = new SpellEntry((ushort)SpellBarManager.SpellBarRows[spellRow].SpellSlot[i].GumpIconSmallID) { X = x, Y = 1 });
            x += 46 + 2;
        }
        
        rowLabel = TextBox.GetOne(spellRow.ToString(), TrueTypeLoader.EMBEDDED_FONT, 12, Color.White, TextBox.RTLOptions.Default());
        rowLabel.X = 482;
        rowLabel.Y = (Height - rowLabel.Height) >> 1;
        Add(rowLabel);
    }

    public class SpellEntry : Control
    {
        private ushort graphic;
        private GumpPic icon;
        public SpellEntry(ushort Graphic)
        {
            graphic = Graphic;
            CanMove = true;
            Width = 46;
            Height = 46;
            Build();
        }

        public void SetSpell(ushort graphic)
        {
            this.graphic = graphic;
            icon.Graphic = graphic;
        }

        public override bool AcceptMouseInput { get; set; } = true;

        private void Build()
        {
            Add(new AlphaBlendControl() { Width = 44, Height = 44, X = 1, Y = 1 });
            Add(icon = new GumpPic(1, 1, graphic, 0));
        }
    }
}