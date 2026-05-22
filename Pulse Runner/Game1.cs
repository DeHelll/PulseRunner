using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Pulse_Runner
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Player _player;
        private Texture2D _bgTexture, _groundTexture;
        private List<Platform> _platforms;

        // Матрица трансформации для камеры
        private Matrix _cameraTransform;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            Window.ClientSizeChanged += OnWindowSizeChanged;
            base.Initialize();
        }

        private void OnWindowSizeChanged(object sender, EventArgs e)
        {
            if (GraphicsDevice != null)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();

                UpdateLevelGeometry();
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _bgTexture = Content.Load<Texture2D>("background");
            _groundTexture = Content.Load<Texture2D>("ground");

            List<Texture2D> runFrames = new List<Texture2D> { Content.Load<Texture2D>("R1"), Content.Load<Texture2D>("R2"), Content.Load<Texture2D>("R3"), Content.Load<Texture2D>("R4"), Content.Load<Texture2D>("R5") };
            List<Texture2D> attackFrames = new List<Texture2D> { Content.Load<Texture2D>("A1"), Content.Load<Texture2D>("A2"), Content.Load<Texture2D>("A3"), Content.Load<Texture2D>("A4"), Content.Load<Texture2D>("A5"), Content.Load<Texture2D>("A6"), Content.Load<Texture2D>("A7"), Content.Load<Texture2D>("A8") };
            Texture2D idleFrame = attackFrames[7];
            Texture2D jumpFrame = Content.Load<Texture2D>("jump");
            Texture2D fallFrame = Content.Load<Texture2D>("fall");

            _player = new Player(idleFrame, runFrames, attackFrames, jumpFrame, fallFrame, null, new Vector2(100, 100), 400f, 0.2f);

            _platforms = new List<Platform>();
            UpdateLevelGeometry();
        }

        private void UpdateLevelGeometry()
        {
            if (_groundTexture == null || _player == null) return;

            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;
            float groundY = h - 100f;

            _player.Scale = 0.22f * ((float)h / 900f);

            _platforms.Clear();

            // Безопасный пол в самом низу
            _platforms.Add(new Platform(new Rectangle(0, (int)groundY, w, 100), _groundTexture));

            int platformWidth = Math.Max(120, w / 4);
            int ph = 40; 
            int gap = 250; 
            
            // Генерируем высокий уровень из 30 платформ зигзагом
            for (int i = 1; i <= 30; i++)
            {
                int px;
                int step = i % 4; // Простая логика зигзага для расстановки
                
                if (step == 1) px = w / 2 - platformWidth / 2;       // Центр
                else if (step == 2) px = w / 6;                      // Лево
                else if (step == 3) px = w / 2 - platformWidth / 2;  // Центр
                else px = w - w / 6 - platformWidth;                 // Право

                _platforms.Add(new Platform(new Rectangle(px, (int)groundY - gap * i, platformWidth, ph), _groundTexture));
            }

            if (_player.Position.Y + _player.HitboxHeight > groundY || _player.Position.X > w)
            {
                _player.Position = new Vector2(w / 2 - _player.HitboxWidth / 2, groundY - _player.HitboxHeight - 5);
                _player.VelocityY = 0f;
                _player.IsOnGround = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var kstate = Keyboard.GetState();
            if (kstate.IsKeyDown(Keys.Escape)) Exit();

            int currentWidth = GraphicsDevice.Viewport.Width;
            int currentHeight = GraphicsDevice.Viewport.Height;

            if (kstate.IsKeyDown(Keys.R))
            {
                _player.Position = new Vector2(currentWidth / 2 - _player.HitboxWidth / 2, currentHeight - 100f - _player.HitboxHeight - 10);
                _player.VelocityY = 0f;
            }

            _player.Update(gameTime, _platforms, currentWidth, currentHeight);

            // === ЛОГИКА КАМЕРЫ ===
            // Вычисляем, насколько нужно сдвинуть мир. Хотим, чтобы игрок был на уровне 60% высоты экрана.
            float targetY = _player.Position.Y - (currentHeight * 0.6f);
            
            // Не даем камере опускаться ниже земли (координата Y смещения не должна быть больше 0)
            if (targetY > 0) targetY = 0;

            // Создаем матрицу сдвига. Так как Y в MonoGame растет вниз, 
            // мы сдвигаем весь мир в отрицательную сторону (вверх), когда игрок поднимается (targetY отрицательный)
            _cameraTransform = Matrix.CreateTranslation(new Vector3(0, -targetY, 0));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            // 1. Отрисовка фона БЕЗ матрицы камеры (фон статичен)
            _spriteBatch.Begin();
            _spriteBatch.Draw(_bgTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            _spriteBatch.End();

            // 2. Отрисовка мира С МАТРИЦЕЙ камеры (платформы и игрок двигаются)
            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            
            foreach (var p in _platforms) 
                p.Draw(_spriteBatch);
                
            _player.Draw(_spriteBatch);
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
