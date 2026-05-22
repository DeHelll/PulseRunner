using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;

namespace Pulse_Runner
{
    public enum PlayerState { Idle, Running, Jumping, Falling, Attacking }

    public class Player
    {
        private Texture2D _idleFrame, _jumpFrame, _fallFrame, _currentTexture;
        private List<Texture2D> _runFrames, _attackFrames;
        private SoundEffect _jumpSound;

        public Vector2 Position;
        public float Speed { get; set; }
        public float Scale { get; set; }
        
        // Физический размер самого персонажа
        public int HitboxWidth => (int)(_idleFrame.Width * Scale * 0.45f); 
        public int HitboxHeight => (int)(_idleFrame.Height * Scale * 0.95f); 
        
        public Rectangle Bounds => new Rectangle((int)Math.Round(Position.X), (int)Math.Round(Position.Y), HitboxWidth, HitboxHeight);

        public float VelocityY = 0f;
        public bool IsOnGround = false;

        private PlayerState _currentState = PlayerState.Idle;
        private int _currentRunFrameIndex = 0, _currentAttackFrameIndex = 0;
        private float _animationTimer = 0f;
        private SpriteEffects _spriteEffect = SpriteEffects.None;
        private MouseState _previousMouseState;

        public Player(Texture2D idleFrame, List<Texture2D> runFrames, List<Texture2D> attackFrames, Texture2D jumpFrame, Texture2D fallFrame, SoundEffect jumpSound, Vector2 position, float speed, float scale)
        {
            _idleFrame = idleFrame; _runFrames = runFrames; _attackFrames = attackFrames;
            _jumpFrame = jumpFrame; _fallFrame = fallFrame; _jumpSound = jumpSound;
            Position = position; Speed = speed; Scale = scale;
            _currentTexture = _idleFrame; _previousMouseState = Mouse.GetState();
        }

        public void Update(GameTime gameTime, List<Platform> platforms, int screenWidth, int screenHeight)
        {
            var kstate = Keyboard.GetState();
            var mstate = Mouse.GetState();
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentState == PlayerState.Attacking)
            {
                UpdateAnimation(time, _attackFrames, ref _currentAttackFrameIndex, 0.08f, true);
                ApplyPhysics(time, platforms);
                _previousMouseState = mstate; 
                return;
            }

            if (mstate.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                _currentState = PlayerState.Attacking; 
                _currentAttackFrameIndex = 0;
                _currentTexture = _attackFrames[0]; 
                _previousMouseState = mstate; 
                return;
            }

            float dx = 0;
            if (kstate.IsKeyDown(Keys.Right) || kstate.IsKeyDown(Keys.D)) { dx = Speed * time; _spriteEffect = SpriteEffects.None; }
            if (kstate.IsKeyDown(Keys.Left) || kstate.IsKeyDown(Keys.A)) { dx = -Speed * time; _spriteEffect = SpriteEffects.FlipHorizontally; }

            Position.X += dx;
            Position.X = MathHelper.Clamp(Position.X, 0, screenWidth - HitboxWidth);
            
            int pxX = (int)Math.Round(Position.X);
            int pxY = (int)Math.Round(Position.Y) + 4;
            Rectangle px = new Rectangle(pxX, pxY, HitboxWidth, HitboxHeight - 8);
            
            foreach (var p in platforms)
            {
                // Возвращаем проверку по p.Bounds (точный размер платформы)
                if (px.Intersects(p.Bounds))
                {
                    if (dx > 0) Position.X = p.Bounds.Left - HitboxWidth;
                    else if (dx < 0) Position.X = p.Bounds.Right;
                }
            }

            bool isFloorBelow = false;
            Rectangle sensor = new Rectangle((int)Math.Round(Position.X) + 4, (int)Math.Round(Position.Y) + HitboxHeight, HitboxWidth - 8, 2);
            foreach (var p in platforms)
            {
                if (sensor.Intersects(p.Bounds))
                {
                    isFloorBelow = true;
                    break;
                }
            }

            if (!isFloorBelow) IsOnGround = false;

            if ((kstate.IsKeyDown(Keys.Up) || kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Space)) && IsOnGround)
            {
                VelocityY = -1100f; 
                IsOnGround = false;
                if (_jumpSound != null) _jumpSound.Play();
            }

            ApplyPhysics(time, platforms);
            UpdateStateAndAnimation(dx != 0, time);
            _previousMouseState = mstate;
        }

        private void ApplyPhysics(float time, List<Platform> platforms)
        {
            if (!IsOnGround)
            {
                VelocityY += 2200f * time; 
                if (VelocityY > 1200f) VelocityY = 1200f;
            }
            else
            {
                VelocityY = 0f; 
            }

            Position.Y += VelocityY * time;

            int pyX = (int)Math.Round(Position.X) + 4;
            int pyY = (int)Math.Round(Position.Y);
            Rectangle py = new Rectangle(pyX, pyY, HitboxWidth - 8, HitboxHeight);

            foreach (var p in platforms)
            {
                if (py.Intersects(p.Bounds))
                {
                    if (VelocityY > 0) 
                    {
                        Position.Y = p.Bounds.Top - HitboxHeight; 
                        VelocityY = 0f;
                        IsOnGround = true;
                    }
                    else if (VelocityY < 0) 
                    {
                        Position.Y = p.Bounds.Bottom;
                        VelocityY = 0f;
                    }
                    
                    py = new Rectangle((int)Math.Round(Position.X) + 4, (int)Math.Round(Position.Y), HitboxWidth - 8, HitboxHeight);
                }
            }
        }

        private void UpdateStateAndAnimation(bool moving, float time)
        {
            if (!IsOnGround) 
            { 
                _currentState = VelocityY < 0 ? PlayerState.Jumping : PlayerState.Falling; 
                _currentTexture = VelocityY < 0 ? _jumpFrame : _fallFrame; 
            }
            else if (moving) 
            { 
                _currentState = PlayerState.Running; 
                UpdateAnimation(time, _runFrames, ref _currentRunFrameIndex, 0.08f, false); 
            }
            else 
            { 
                _currentState = PlayerState.Idle; 
                _currentTexture = _idleFrame; 
            }
        }

        private void UpdateAnimation(float time, List<Texture2D> frames, ref int index, float speed, bool stopAtEnd)
        {
            _animationTimer += time;
            if (_animationTimer >= speed)
            {
                _animationTimer = 0f; 
                index++;
                if (index >= frames.Count)
                {
                    if (stopAtEnd) { _currentState = PlayerState.Idle; _currentTexture = _idleFrame; index = 0; }
                    else index = 0;
                }
                if (_currentState != PlayerState.Idle) _currentTexture = frames[index];
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float targetVisualHeight = _idleFrame.Height * Scale;
            float dynScale = targetVisualHeight / _currentTexture.Height;
            if (_currentState == PlayerState.Running || _currentState == PlayerState.Jumping || _currentState == PlayerState.Falling) 
            {
                dynScale *= 1.2f;
            }

            float visualWidth = _currentTexture.Width * dynScale;
            float visualHeight = _currentTexture.Height * dynScale;
            
            float wOff = (HitboxWidth - visualWidth) / 2f;
            float hOff = HitboxHeight - visualHeight; 
            
            // ВАЖНО: Если персонаж висит в воздухе над платформой, увеличь feetOffset (например до 15f или 20f). 
            // Если он проваливается ногами в траву, сделай его отрицательным (например -5f).
            float feetOffset = 10f; 
            
            Vector2 drawPos = new Vector2((float)Math.Round(Position.X + wOff), (float)Math.Round(Position.Y + hOff + feetOffset));
            spriteBatch.Draw(_currentTexture, drawPos, null, Color.White, 0f, Vector2.Zero, dynScale, _spriteEffect, 0f);
        }
    }
}
