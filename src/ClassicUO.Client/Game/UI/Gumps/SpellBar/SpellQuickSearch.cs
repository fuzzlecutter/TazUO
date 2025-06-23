using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellQuickSearch : Gump
{
    private TTFTextInputField searchField;

    private SpellDisplay spellDisplay;
    
    public SpellQuickSearch(int x, int y) : base(0, 0)
    {
        Width = 200;
        Height = 69;
        CanMove = true;
        X = x;
        Y = y;

        Build();
    }

    public override bool AcceptMouseInput { get; set; } = true;

    private void Build()
    {
        Add(new AlphaBlendControl(0.75f){Width = Width, Height = Height});
        
        Add(searchField = new TTFTextInputField(Width, 25, Width){Y = Height - 25});
        searchField.TextChanged += SearchTextChanged;
        
        Add(spellDisplay = new(Width){Y = Height - 25 - 44});
    }

    private void SearchTextChanged(object sender, EventArgs e)
    {
        if(searchField.Text.Length > 0)
            if (SpellDefinition.TryGetSpellFromName(searchField.Text, out var spell))
            {
                spellDisplay.SetSpell(spell);
            }
    }

    private class SpellDisplay : Control
    {
        private GumpPic icon;
        private TextBox text;
        public SpellDisplay(int width)
        {
            Width = width;
            Height = 44;
            CanMove = true;
        }
        
        public override bool AcceptMouseInput { get; set; } = true;

        public void SetSpell(SpellDefinition spell)
        {
            icon?.Dispose();
            //Intential to only use height, these are square spell icons.
            Add(icon = new GumpPic(0, 0, (ushort)spell.GumpIconSmallID, 0) { AcceptMouseInput = false, Width = Height, Height = Height });
            Color color = Color.White;

            switch (spell.TargetType)
            {
                case TargetType.Harmful: color = Color.OrangeRed; break;

                case TargetType.Beneficial: color = Color.GreenYellow; break;
            }
            
            text?.Dispose();
            Add(text = TextBox.GetOne(spell.Name, TrueTypeLoader.EMBEDDED_FONT, 18, color, TextBox.RTLOptions.Default(Width - Height)));
            text.X = Height;
            text.Y = (Height - text.Height) >> 1;
        }
    }
}