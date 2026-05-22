using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pulse_Runner
{
    public class Platform
    {
        public Rectangle Bounds { get; set; }
        public Texture2D Texture { get; set; }

        public Platform(Rectangle bounds, Texture2D texture)
        {
            Bounds = bounds;
            Texture = texture;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }
}
