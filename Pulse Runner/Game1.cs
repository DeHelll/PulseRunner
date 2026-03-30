using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Pulse_Runner
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Player _player;
        private Texture2D _bgTexture;
        private Texture2D _groundTexture;

        private KeyboardState _previousKeyboardState;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            //Full HD
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _bgTexture = Content.Load<Texture2D>("background");
            _groundTexture = Content.Load<Texture2D>("ground");

            List<Texture2D> runFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("R1"), Content.Load<Texture2D>("R2"),
                Content.Load<Texture2D>("R3"), Content.Load<Texture2D>("R4"),
                Content.Load<Texture2D>("R5")
            };

            List<Texture2D> attackFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("A1"), Content.Load<Texture2D>("A2"),
                Content.Load<Texture2D>("A3"), Content.Load<Texture2D>("A4"),
                Content.Load<Texture2D>("A5"), Content.Load<Texture2D>("A6"),
                Content.Load<Texture2D>("A7"), Content.Load<Texture2D>("A8")
            };

            Texture2D idleFrame = attackFrames[7];
            Texture2D jumpFrame = Content.Load<Texture2D>("jump");
            Texture2D fallFrame = Content.Load<Texture2D>("fall");

            float groundY = GraphicsDevice.Viewport.Height - 130f;
            float playerScale = 0.5f;
            float startY = groundY - (idleFrame.Height * playerScale);

            _player = new Player(idleFrame, runFrames, attackFrames, jumpFrame, fallFrame, new Vector2(200, startY), 400f, playerScale);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            //j switch between Full HD and HD modes
            if (currentKeyboardState.IsKeyDown(Keys.J) && _previousKeyboardState.IsKeyUp(Keys.J))
            {
                if (_graphics.IsFullScreen)
                {
                    //Full HD -> HD 
                    _graphics.PreferredBackBufferWidth = 1280;
                    _graphics.PreferredBackBufferHeight = 720;
                    _graphics.IsFullScreen = false;
                }
                else
                {
                    // HD -> Full HD 
                    _graphics.PreferredBackBufferWidth = 1920;
                    _graphics.PreferredBackBufferHeight = 1080;
                    _graphics.IsFullScreen = true;
                }
                _graphics.ApplyChanges(); 
            }

            float groundY = GraphicsDevice.Viewport.Height - 130f;
            float floorY = groundY - _player.Height;

            int currentScreenWidth = GraphicsDevice.Viewport.Width;
            _player.Update(gameTime, floorY, currentScreenWidth);

            _previousKeyboardState = currentKeyboardState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            int currentWidth = GraphicsDevice.Viewport.Width;
            int currentHeight = GraphicsDevice.Viewport.Height;

            _spriteBatch.Draw(_bgTexture, new Rectangle(0, 0, currentWidth, currentHeight), Color.White);

            float groundDrawY = currentHeight - 630f;
            _spriteBatch.Draw(_groundTexture, new Vector2(0, groundDrawY), Color.White);

            _player.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}