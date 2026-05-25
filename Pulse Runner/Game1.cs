using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace Pulse_Runner
{
    public class Game1 : Game
    {

        private RenderTarget2D _renderTarget;
// Укажи здесь базовое разрешение, под которое ты делал игру
private int _virtualWidth = 1280; 
private int _virtualHeight = 720;
        private Texture2D _lavaTexture;
        private float _lavaTextureOffset = 0f;
        private Texture2D _coinTexture;
        private bool _coinPlaced = false;
        public bool IsWon = false;
        private float _lavaY;
        private float _lavaSpeed = 325f; // Уменьшил скорость, чтобы играть было реально
        private float _startDelay = 1.2f;
        private bool _isGameOver = false;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Player _player;
        private Texture2D _bgTexture, _platformTexture;
        private List<Platform> _platforms;
        private SpriteFont _font;
        private float _finishY;
        private SoundEffect _jumpSound;
        private SoundEffect _victorySound;
        private Song _bgMusic;

        private Matrix _cameraTransform;
        private const float ReferenceHeight = 1080f;
        private const float BasePlayerScale = 0.25f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // Вернул ваш закомментированный код для автоматического определения размера экрана
            int screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            int targetHeight = screenHeight;
            int targetWidth = (int)(targetHeight * 0.75f);

            _graphics.PreferredBackBufferWidth = targetWidth;
            _graphics.PreferredBackBufferHeight = targetHeight;

            Window.IsBorderless = true;
            Window.AllowUserResizing = false;

            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();
            int screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            // Центрируем окно по ширине
            Window.Position = new Point((screenWidth - _graphics.PreferredBackBufferWidth) / 2, 0);
        }

        protected override void LoadContent()
        {

            
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _bgTexture = Content.Load<Texture2D>("background");
            _platformTexture = Content.Load<Texture2D>("platform");

            _coinTexture = Content.Load<Texture2D>("coin");

            try { _font = Content.Load<SpriteFont>("font"); } catch { }

            try
            {
                _bgMusic = Content.Load<Song>("bgm");
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = 0.05f;
                MediaPlayer.Play(_bgMusic);
            }
            catch { Console.WriteLine("File bgm.mp3 is not found."); }

            try { _jumpSound = Content.Load<SoundEffect>("jump_sfx"); } catch { }
            try { _victorySound = Content.Load<SoundEffect>("victory_sfx"); } catch { Console.WriteLine("File victory_sfx.wav is not found."); }

            // Загрузка спрайтов (полагаем, что Player принимает frames[7] как idle)
            List<Texture2D> runFrames = new List<Texture2D> { Content.Load<Texture2D>("R1"), Content.Load<Texture2D>("R2"), Content.Load<Texture2D>("R3"), Content.Load<Texture2D>("R4"), Content.Load<Texture2D>("R5") };
            List<Texture2D> attackFrames = new List<Texture2D> { Content.Load<Texture2D>("A1"), Content.Load<Texture2D>("A2"), Content.Load<Texture2D>("A3"), Content.Load<Texture2D>("A4"), Content.Load<Texture2D>("A5"), Content.Load<Texture2D>("A6"), Content.Load<Texture2D>("A7"), Content.Load<Texture2D>("A8") };
            Texture2D idleFrame = attackFrames[7];
            Texture2D jumpFrame = Content.Load<Texture2D>("jump");
            Texture2D fallFrame = Content.Load<Texture2D>("fall");

            _player = new Player(idleFrame, runFrames, attackFrames, jumpFrame, fallFrame, _jumpSound, _victorySound, new Vector2(100, 100), 400f, BasePlayerScale);

            // ИСПРАВЛЕНИЕ: Удалена двойная загрузка лавы
            _lavaTexture = Content.Load<Texture2D>("lava");

            int h = _graphics.PreferredBackBufferHeight;
            float groundY = h - 66.2f;

            _lavaY = groundY + 100f; // Лава появляется ниже стартового пола

            _platforms = new List<Platform>();

            UpdateLevelGeometry();
        }

        private void UpdateLevelGeometry()
        {
            if (_platformTexture == null || _player == null) return;

            int w = _graphics.PreferredBackBufferWidth;
            int h = _graphics.PreferredBackBufferHeight;
            float groundY = h - 66.2f;

            _player.Scale = BasePlayerScale * ((float)h / ReferenceHeight);

            _platforms.Clear();

            int platformWidth = 200;
            int ph = 20;

            int minGapY = _player.HitboxHeight + 20;
            int maxGapY = 240;
            int maxHorizontalReach = 350;

            _platforms.Add(new Platform(new Rectangle(0, (int)groundY, w, 100), _platformTexture));

            Random rng = new Random();
            List<int> previousTierX = new List<int>() { w / 2 };
            int currentY = (int)groundY;

            int consecutiveSingles = 0;
            int consecutiveDoubles = 0;

            for (int i = 1; i <= 100; i++)
            {
                currentY -= rng.Next(minGapY, maxGapY);

                int platformsCount;
                if (consecutiveSingles >= 2) platformsCount = 2;
                else if (consecutiveDoubles >= 2) platformsCount = 1;
                else platformsCount = (rng.Next(1, 101) <= 60) ? 1 : 2;

                if (platformsCount == 1) { consecutiveSingles++; consecutiveDoubles = 0; }
                else { consecutiveDoubles++; consecutiveSingles = 0; }

                List<int> currentTierX = new List<int>();

                for (int p = 0; p < platformsCount; p++)
                {
                    int px = 0;
                    bool validPosition = false;
                    int attempts = 0;

                    while (!validPosition && attempts < 20)
                    {
                        attempts++;
                        px = rng.Next(10, w - platformWidth - 10);

                        bool isReachable = false;
                        bool isBlocking = false;

                        foreach (int prevX in previousTierX)
                        {
                            int dist = Math.Abs(px - prevX);

                            int centerPrev = prevX + platformWidth / 2;
                            int centerCurrent = px + platformWidth / 2;
                            if (Math.Abs(centerCurrent - centerPrev) <= maxHorizontalReach)
                                isReachable = true;

                            if (dist < platformWidth + 20)
                                isBlocking = true;
                        }

                        bool isOverlapping = false;
                        foreach (int currX in currentTierX)
                        {
                            if (Math.Abs(currX - px) < platformWidth + _player.HitboxWidth)
                            {
                                isOverlapping = true;
                                break;
                            }
                        }

                        if (isReachable && !isBlocking && !isOverlapping)
                            validPosition = true;
                    }

                    if (!validPosition && previousTierX.Count > 0)
                    {
                        px = previousTierX[0] + platformWidth + 20;
                        if (px > w - platformWidth - 10)
                            px = previousTierX[0] - platformWidth - 20;
                        px = Math.Clamp(px, 10, w - platformWidth - 10);
                    }

                    currentTierX.Add(px);
                    _platforms.Add(new Platform(new Rectangle(px, currentY, platformWidth, ph), _platformTexture));
                }

                previousTierX = currentTierX;
            }

            float minPlatformY = float.MaxValue;
            foreach (var p in _platforms)
            {
                if (p.Bounds.Y < minPlatformY)
                {
                    minPlatformY = p.Bounds.Y;
                }
            }
            _finishY = minPlatformY;

            _player.Position = new Vector2(w / 2 - _player.HitboxWidth / 2, groundY - _player.HitboxHeight);
            _player.VelocityY = 0f;
            _player.IsOnGround = true;
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
            if (_isGameOver) return;

            int currentWidth = _graphics.PreferredBackBufferWidth;
            int currentHeight = _graphics.PreferredBackBufferHeight;
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _player.Update(gameTime, _platforms, currentWidth, currentHeight);

            // Логика камеры (центрирована по вертикали с небольшим смещением вниз)
            float targetY = _player.Position.Y - (currentHeight * 0.6f);
            if (targetY > 0) targetY = 0; // Камера не опускается ниже стартового пола
            _cameraTransform = Matrix.CreateTranslation(new Vector3(0, -targetY, 0));

            // Анимация лавы
            _lavaTextureOffset += 60f * deltaTime;

            // Логика задержки старта лавы
            if (_startDelay > 0)
            {
                _startDelay -= deltaTime;
            }
            else
            {
                _lavaY -= _lavaSpeed * deltaTime;
            }

            // Условие проигрыша
            if (_player.Position.Y + _player.HitboxHeight > _lavaY)
            {
                _isGameOver = true;
            }



            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // 1. Статический фон (не двигается с камерой)
            _spriteBatch.Begin();

            float bgScaleX = (float)_graphics.PreferredBackBufferWidth / _bgTexture.Width;
            float bgScaleY = (float)_graphics.PreferredBackBufferHeight / _bgTexture.Height;
            float bgScale = Math.Max(bgScaleX, bgScaleY);

            float bgWidth = _bgTexture.Width * bgScale;
            float bgHeight = _bgTexture.Height * bgScale;
            float bgPosX = (_graphics.PreferredBackBufferWidth - bgWidth) / 2f;
            float bgPosY = (_graphics.PreferredBackBufferHeight - bgHeight) / 2f;

            _spriteBatch.Draw(_bgTexture, new Vector2(bgPosX, bgPosY), null, Color.White, 0f, Vector2.Zero, bgScale, SpriteEffects.None, 0f);

            if (_font != null)
            {
                int distanceToFinish = (int)Math.Max(0, (_player.Position.Y + _player.HitboxHeight) - _finishY);
                _spriteBatch.DrawString(_font, $"Distance to Finish: {distanceToFinish}m", new Vector2(20, 20), Color.White);

                if (_player.IsWon)
                {
                    string winText = "VICTORY!";
                    Vector2 size = _font.MeasureString(winText);
                    _spriteBatch.DrawString(_font, winText, new Vector2((_graphics.PreferredBackBufferWidth - size.X) / 2, _graphics.PreferredBackBufferHeight / 2), Color.Gold);
                }
            }
            _spriteBatch.End();

            // 2. Мировые объекты (двигаются с камерой)
            _spriteBatch.Begin(
                samplerState: SamplerState.LinearWrap,
                transformMatrix: _cameraTransform
            );

            // 1. Сначала рисуем платформы
            foreach (var p in _platforms) p.Draw(_spriteBatch);

            // 2. Рисуем игрока (если хочешь, чтобы лава перекрывала и его, ставь этот код после лавы)
            _player.Draw(_spriteBatch);

            // 3. ЛАВА ПОСЛЕДНЯЯ: теперь она будет «поверх» всего, что нарисовано выше
            if (_lavaTexture != null)
            {
                int screenWidth = _graphics.PreferredBackBufferWidth;
                Rectangle destRect = new Rectangle(0, (int)_lavaY, screenWidth, 2000);
                Rectangle sourceRect = new Rectangle((int)_lavaTextureOffset, 0, screenWidth, 2000);

                _spriteBatch.Draw(_lavaTexture, destRect, sourceRect, Color.White);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}