// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Utility;
using SDL3;

namespace ClassicUO.Game.UI.Gumps.Login
{
    [Flags]
    public enum LoginButtons
    {
        None = 1,
        OK = 2,
        Cancel = 4
    }

    public class LoadingGump : Gump
    {
        private readonly Action<int> _buttonClick;
        private readonly Label _label;

        public LoadingGump(World world, string labelText, LoginButtons showButtons, Action<int> buttonClick = null) : base(world, 0, 0)
        {
            _buttonClick = buttonClick;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);

            var originalStyle = Client.Game.UO.Version <= ClientVersion.CV_12535;
            var okayButton = (ushort)(originalStyle ? 0x00F7 : 0x0481);
            var cancelButton = (ushort)(originalStyle ? 0x00F1 : 0x047E);

            _label = new Label
            (
                labelText,
                unicode,
                hue,
                326,
                font,
                align: TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 162,
                Y = 178
            };

            Add
            (
                new ResizePic(0x0A28)
                {
                    X = 142, Y = 134, Width = 366, Height = 212
                }
            );

            Add(_label);

            if (showButtons == LoginButtons.OK)
            {
                Add
                (
                    new Button((int) LoginButtons.OK, (ushort)(okayButton + 2), (ushort)(okayButton + 1), okayButton)
                    {
                        X = 306, Y = 304, ButtonAction = ButtonAction.Activate
                    }
                );
            }
            else if (showButtons == LoginButtons.Cancel)
            {
                Add
                (
                    new Button((int) LoginButtons.Cancel, (ushort)(cancelButton + 2), cancelButton, (ushort)(cancelButton + 1))
                    {
                        X = 306,
                        Y = 304,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }
            else if (showButtons == (LoginButtons.OK | LoginButtons.Cancel))
            {
                Add
                (
                    new Button((int) LoginButtons.OK, (ushort)(okayButton + 2), (ushort)(okayButton + 1), okayButton)
                    {
                        X = 264, Y = 304, ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Button((int) LoginButtons.Cancel, (ushort)(cancelButton + 2), cancelButton, (ushort)(cancelButton + 1))
                    {
                        X = 348, Y = 304, ButtonAction = ButtonAction.Activate
                    }
                );
            }
        }

        public void SetText(string text) => _label.Text = text;

        public override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_KP_ENTER || key == SDL.SDL_Keycode.SDLK_RETURN)
            {
                OnButtonClick((int) LoginButtons.OK);
            }
        }


        public override void OnButtonClick(int buttonID)
        {
            _buttonClick?.Invoke(buttonID);
            base.OnButtonClick(buttonID);
        }
    }
}
