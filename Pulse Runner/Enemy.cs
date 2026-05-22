using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pulse_Runner
{
    public enum EnemyState { Idle, Patrol }

    public class Enemy
    {
        public Vector2 Position;
        public Texture2D Texture;
        public float Speed;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);

        private EnemyState _currentState;
        private float _minX, _maxX;
        private int _direction = 1;
        private float _stateTimer = 0f;

        public Enemy(Texture2D texture, Vector2 startPosition, float patrolDistance, float speed)
        {
            Texture = texture;
            Position = startPosition;
            Speed = speed;
            _minX = startPosition.X;
            _maxX = startPosition.X + patrolDistance;
            _currentState = EnemyState.Patrol;
        }

        public void Update(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _stateTimer += time;

            switch (_currentState)
            {
                case EnemyState.Idle:
                    if (_stateTimer > 2f)
                    {
                        _currentState = EnemyState.Patrol;
                        _stateTimer = 0f;
                    }
                    break;

                case EnemyState.Patrol:
                    Position.X += Speed * _direction * time;
                    if (Position.X >= _maxX || Position.X <= _minX)
                    {
                        Position.X = MathHelper.Clamp(Position.X, _minX, _maxX);
                        _direction *= -1;
                        _currentState = EnemyState.Idle;
                        _stateTimer = 0f;
                    }
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects effect = _direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, 1f, effect, 0f);
        }
    }
}
