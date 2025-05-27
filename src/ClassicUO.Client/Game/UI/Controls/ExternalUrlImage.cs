using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    public class ExternalUrlImage : Control
    {
        private Texture2D _texture;
        private bool _loading;
        private Vector3 _hue;

        public ExternalUrlImage(string url, int width = 100, int height = 100)
        {
            Width = width;
            Height = height;
            _hue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            LoadImageFromUrl(url);
        }

        public void LoadImageFromUrl(string url)
        {
            _loading = true;

            Task.Run(() => LoadImage(url));
        }

        private void LoadImage(string url)
        {
            try
            {
                using var client = new WebClient();
                byte[] data = client.DownloadData(url);

                using var ms = new MemoryStream(data);
                var texture = Texture2D.FromStream(Client.Game.GraphicsDevice, ms);

                _texture?.Dispose();
                _texture = texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from URL: {ex}");
            }
            finally
            {
                _loading = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _texture?.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_texture != null && !_loading)
            {
                batcher.Draw(_texture, new Rectangle(x, y, Width, Height), _texture.Bounds, _hue);
            }

            return true;
        }
    }
}