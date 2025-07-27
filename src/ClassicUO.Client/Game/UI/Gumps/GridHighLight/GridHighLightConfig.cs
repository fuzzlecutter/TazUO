using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer.Lights;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightConfig : Gump
    {
        private const int WIDTH = 350, HEIGHT = 500;
        private int lastYitem = 0;
        private int lastXitem = 0;

        public GridHighlightConfig(int x, int y) : base(0, 0)
        {
            Width = (175 + 2) * 6;
            Height = HEIGHT;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            var originalStyle = Client.Version <= ClientVersion.CV_12535;
            var inputBoxStyle = (ushort)(originalStyle ? 0x0A3C : 0x0BB8);

            Add(new AlphaBlendControl(0.85f) { Width = Width, Height = HEIGHT });

            Label label;
            Add(label = new Label("Properties configuration (separated by a new line)", true, 0xffff) { X = 0, Y = lastYitem });

            lastYitem += 20;

            List<(string Label, HashSet<string> Set)> categories = new()
                {
                    ("Properties", GridHighlightRules.Properties),
                    ("Super slayers", GridHighlightRules.SuperSlayerProperties),
                    ("Slayers", GridHighlightRules.SlayerProperties),
                    ("Resistances", GridHighlightRules.Resistances),
                    ("Negatives", GridHighlightRules.NegativeProperties),
                    ("Rarity", GridHighlightRules.RarityProperties)
                };
            foreach (var (labelText, propSet) in categories)
            {
                Add(label = new Label(labelText, true, 0xffff) { X = lastXitem, Y = lastYitem });
                ScrollArea propertiesScrollArea;
                InputField propertiesPropInput;
                Add(propertiesScrollArea = new ScrollArea(lastXitem, lastYitem + 20, 175, HEIGHT - lastYitem - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
                propertiesScrollArea.Add(propertiesPropInput = new InputField(inputBoxStyle, 0xFF, 0xFFFF, true, 175 - 13, (HEIGHT - lastYitem - 20) * 10) { Y = 0 });
                string s = string.Join("\n", propSet);
                propertiesPropInput.SetText(s);
                CancellationTokenSource cts = new CancellationTokenSource();
                propertiesPropInput.TextChanged += async (s, e) =>
                {
                    CancellationTokenSource oldToken = cts;
                    oldToken?.Cancel();
                    cts = new CancellationTokenSource();
                    var token = cts.Token;

                    try
                    {
                        await Task.Delay(500, token);
                        if (!token.IsCancellationRequested)
                        {
                            var text = propertiesPropInput.Text;
                            var parsed = text
                                .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrEmpty(p))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            ProfileManager.CurrentProfile.ConfigurableProperties = parsed;
                            GridHighlightRules.SaveGridHighlightConfiguration();
                            propertiesPropInput.Add(new FadingLabel(10, "Saved", true, 0xff) { X = 0, Y = 0 });
                        }
                    }
                    catch (TaskCanceledException) { }
                    oldToken?.Dispose();
                };

                lastXitem += 175 + 2;
            }
        }
    }
}
