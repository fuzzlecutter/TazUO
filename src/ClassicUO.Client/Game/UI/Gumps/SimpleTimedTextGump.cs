using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SimpleTimedTextGump : Gump
    {
        private readonly DateTime expireAt;

        public SimpleTimedTextGump(string text, Color color, TimeSpan duration) : base(0, 0)
        {
            expireAt = DateTime.Now.Add(duration);
            TextBox t;
            Add(t = TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, 20, color, TextBox.RTLOptions.Default()));
            Height = t.MeasuredSize.Y;
            Width = t.MeasuredSize.X;
            WantUpdateSize = true;
        }

        public SimpleTimedTextGump(string text, uint hue, TimeSpan duration) : base(0, 0)
        {
            expireAt = DateTime.Now.Add(duration);
            TextBox t;
            Add(t = TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, 20, (int)hue, TextBox.RTLOptions.Default()));
            Height = t.MeasuredSize.Y;
            Width = t.MeasuredSize.X;
            WantUpdateSize = true;
        }

        public SimpleTimedTextGump(string text, uint hue, TimeSpan duration, int width) : base(0, 0)
        {
            expireAt = DateTime.Now.Add(duration);
            TextBox t;
            Add(t = TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, 20, (int)hue, new TextBox.RTLOptions() { Width = width }));
            Height = t.MeasuredSize.Y;
            Width = t.MeasuredSize.X;
            WantUpdateSize = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DateTime.Now >= expireAt)
                Dispose();

            return base.Draw(batcher, x, y);
        }
    }
}
