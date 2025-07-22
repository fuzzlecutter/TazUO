// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    public enum MessageButtonType
    {
        OK,
        OK_CANCEL,

    }

    public class MessageBoxGump : Gump
    {
        private readonly Action<bool> _action;

        public MessageBoxGump(World world, int w, int h, string message, Action<bool> action, bool hasBackground = false, MessageButtonType menuType = MessageButtonType.OK) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;

            IsModal = true;
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

            Width = w;
            Height = h;
            _action = action;
            var originalStyle = Client.Game.UO.Version <= ClientVersion.CV_12535;
            var okayButton = (ushort)(originalStyle ? 0x00F7 : 0x0481);
            var cancelButton = (ushort)(originalStyle ? 0x00F1 : 0x047E);

            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = w, Height = h
                }
            );

            if (hasBackground)
            {
                var background = new ResizePic(3000)
                {
                    X = X + 30,
                    Y = Y + 40,
                    Width = Width - 60,
                    Height = Height - 100
                };

                Add(background);
            }

            Add
            (
                new Label
                (
                    message,
                    false,
                    0x0386,
                    Width - 90,
                    1
                )
                {
                    X = 40,
                    Y = 45
                }
            );

            X = (ScaleHelper.LogicalWindowWidth - Width) >> 1;
            Y = (ScaleHelper.LogicalWindowHeight - Height) >> 1;

            // OK
            Button b;

            Add
            (
                b = new Button(0, (ushort)(okayButton + 2), (ushort)(okayButton + 1), okayButton)
                {
                    Y = Height - 45,
                    ButtonAction = ButtonAction.Activate
                }
            );

            b.X = (Width - b.Width) >> 1;


            if (menuType == MessageButtonType.OK_CANCEL)
            {
                Button bCancel;

                Add
                (
                    bCancel = new Button(1, (ushort)(cancelButton + 2), cancelButton, (ushort)(cancelButton + 1))
                    {
                        Y = Height - 45,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                b.X = Width / 2 - bCancel.Width;
                bCancel.X = Width / 2 - bCancel.Width;
                bCancel.X += b.Width + 5;
            }


            WantUpdateSize = false;

            UIManager.KeyboardFocusControl = this;
            UIManager.KeyboardFocusControl.SetKeyboardFocus();
        }

        public override void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            base.OnKeyUp(key, mod);

            if (key == SDL.SDL_Keycode.SDLK_RETURN && mod == 0)
            {
                OnButtonClick(0);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action?.Invoke(true);
                    Dispose();

                    break;

                case 1:
                    _action?.Invoke(false);
                    Dispose();

                    break;
            }
        }
    }


    public class EntryDialog : Gump
    {
        private readonly Action<string> _action;
        private readonly StbTextBox _textBox;

        public EntryDialog(World world, int w, int h, string message, Action<string> action) : base(world, 0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = false;

            IsModal = true;
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

            Width = w;
            Height = h;
            _action = action;

            var originalStyle = Client.Game.UO.Version <= ClientVersion.CV_12535;
            var okayButton = (ushort)(originalStyle ? 0x00F7 : 0x0481);

            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = w,
                    Height = h
                }
            );

            Label l;

            Add
            (
                l = new Label
                (
                    message,
                    false,
                    0x0386,
                    Width - 90,
                    1
                )
                {
                    X = 40,
                    Y = 45
                }
            );

            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = 40,
                    Y = 45 + l.Height + 5,
                    Width = w - 90,
                    Height = 25
                }
            );

            int ww = w - 94;

            _textBox = new StbTextBox
            (
                0xFF,
                -1,
                ww,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed
            )
            {
                X = 42,
                Y = 45 + l.Height + 7,
                Width = ww,
                Height = 25
            };

            Add(_textBox);

            X = (ScaleHelper.LogicalWindowWidth - Width) >> 1;
            Y = (ScaleHelper.LogicalWindowHeight - Height) >> 1;


            // OK
            Button b;

            Add
            (
                b = new Button(0, (ushort)(okayButton + 2), (ushort)(okayButton + 1), okayButton)
                {
                    Y = Height - 45,
                    ButtonAction = ButtonAction.Activate
                }
            );

            b.X = (Width - b.Width) >> 1;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action?.Invoke(_textBox.Text);
                    Dispose();

                    break;
            }
        }
    }
}
