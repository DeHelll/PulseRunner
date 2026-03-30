using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Pulse_Runner
{
    public enum PlayerState { Idle, Running, Jumping, Falling, Attacking }

    public class Player
    {
        private Texture2D _idleFrame;
        private List<Texture2D> _runFrames;
        private List<Texture2D> _attackFrames;
        private Texture2D _jumpFrame;
        private Texture2D _fallFrame;
        private Texture2D _currentTexture;

        public Vector2 Position;
        public float Speed { get; set; }
        public float Scale { get; set; }

        public float Height => _idleFrame.Height * Scale;
        public float Width => _idleFrame.Width * Scale;

        public float VelocityY = 0f;
        public bool IsOnGround = false;

        private PlayerState _currentState;
        private int _currentRunFrameIndex = 0;
        private int _currentAttackFrameIndex = 0;
        private float _animationTimer = 0f;

        private float _runFrameTime = 0.08f;

       
        private float _attackFrameTime = 0.08f;

        private SpriteEffects _spriteEffect = SpriteEffects.None;

        //mouse state tracking for click detection
        private MouseState _previousMouseState;

        public Player(Texture2D idleFrame, List<Texture2D> runFrames, List<Texture2D> attackFrames, Texture2D jumpFrame, Texture2D fallFrame, Vector2 position, float speed, float scale)
        {
            _idleFrame = idleFrame;
            _runFrames = runFrames;
            _attackFrames = attackFrames;
            _jumpFrame = jumpFrame;
            _fallFrame = fallFrame;
            Position = position;
            Speed = speed;
            Scale = scale;

            _currentTexture = _idleFrame;
            _currentState = PlayerState.Idle;

            _previousMouseState = Mouse.GetState(); 
        }

        public void Update(GameTime gameTime, float floorY, int screenWidth)
        {
            var kstate = Keyboard.GetState();
            var mstate = Mouse.GetState();
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool isMovingHorizontally = false;

            // attack state 
            if (_currentState == PlayerState.Attacking)
            {
                UpdateAttackAnimation(gameTime);

                VelocityY += 1500f * time;
                Position.Y += VelocityY * time;
                if (Position.Y >= floorY) { Position.Y = floorY; VelocityY = 0f; IsOnGround = true; }
                else { IsOnGround = false; }
                _previousMouseState = mstate;
                return;
            }

            bool isMouseClicked = mstate.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            if (isMouseClicked && _currentState != PlayerState.Attacking)
            {
                _currentState = PlayerState.Attacking;
                _currentAttackFrameIndex = 0;
                _animationTimer = 0f;
                _currentTexture = _attackFrames[0];

                _previousMouseState = mstate; 
                return;
            }

            // movement 
            if (kstate.IsKeyDown(Keys.Right) || kstate.IsKeyDown(Keys.D))
            {
                Position.X += Speed * time;
                isMovingHorizontally = true;
                _spriteEffect = SpriteEffects.None;
            }
            if (kstate.IsKeyDown(Keys.Left) || kstate.IsKeyDown(Keys.A))
            {
                Position.X -= Speed * time;
                isMovingHorizontally = true;
                _spriteEffect = SpriteEffects.FlipHorizontally;
            }

            if (Position.X < 0) Position.X = 0;
            if (Position.X > screenWidth - Width) Position.X = screenWidth - Width;

            VelocityY += 1500f * time;
            Position.Y += VelocityY * time;

            if (Position.Y >= floorY)
            {
                Position.Y = floorY;
                VelocityY = 0f;
                IsOnGround = true;
            }
            else
            {
                IsOnGround = false;
            }

            if ((kstate.IsKeyDown(Keys.Up) || kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Space)) && IsOnGround)
            {
                VelocityY = -750f;
                IsOnGround = false;
            }

            if (!IsOnGround)
            {
                if (VelocityY < 0) { _currentState = PlayerState.Jumping; _currentTexture = _jumpFrame; }
                else { _currentState = PlayerState.Falling; _currentTexture = _fallFrame; }
            }
            else
            {
                if (isMovingHorizontally)
                {
                    _currentState = PlayerState.Running;
                    UpdateRunAnimation(gameTime);
                }
                else
                {
                    _currentState = PlayerState.Idle;
                    _currentTexture = _idleFrame;
                    _currentRunFrameIndex = 0;
                }
            }
            _previousMouseState = mstate;
        }

        private void UpdateRunAnimation(GameTime gameTime)
        {
            _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_animationTimer >= _runFrameTime)
            {
                _currentRunFrameIndex++;
                _animationTimer = 0f;
                if (_currentRunFrameIndex >= _runFrames.Count) _currentRunFrameIndex = 0;
                _currentTexture = _runFrames[_currentRunFrameIndex];
            }
        }

        private void UpdateAttackAnimation(GameTime gameTime)
        {
            _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_animationTimer >= _attackFrameTime)
            {
                _currentAttackFrameIndex++;
                _animationTimer = 0f;

                if (_currentAttackFrameIndex >= _attackFrames.Count)
                {
                    _currentState = PlayerState.Idle;
                    _currentTexture = _idleFrame;
                    _currentAttackFrameIndex = 0;
                }
                else
                {
                    _currentTexture = _attackFrames[_currentAttackFrameIndex];
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float targetScreenHeight = _idleFrame.Height * Scale;
            float dynamicScale = targetScreenHeight / _currentTexture.Height;

            if (_currentState == PlayerState.Running || _currentState == PlayerState.Jumping || _currentState == PlayerState.Falling)
            {
                dynamicScale *= 1.2f; // scale change for run/jump/fall cos of stupid sprite sizes
            }

            float currentWidth = _currentTexture.Width * dynamicScale;
            float currentHeight = _currentTexture.Height * dynamicScale;

            float widthOffset = (_idleFrame.Width * Scale - currentWidth) / 2f;
            float heightOffset = (_idleFrame.Height * Scale - currentHeight);

            Vector2 drawPosition = new Vector2(Position.X + widthOffset, Position.Y + heightOffset);

            spriteBatch.Draw(_currentTexture, drawPosition, null, Color.White, 0f, Vector2.Zero, dynamicScale, _spriteEffect, 0f);
        }
    }
}