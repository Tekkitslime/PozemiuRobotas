using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas
{
    public class Player(TileObject tobj, Texture2D texture, Rectangle srcRect) : GameEntity(tobj, texture, srcRect)
    {
        private readonly int tSize = 16;
        public List<int> DoorsUnlocked = [];

        public Action? OnMoved;
        public Func<Vector2, bool>? IsTileSolid;
        
        public Vector2 TargetPosition { get; set; } = new(tobj.X, tobj.Y);

        public override void Update(float dt)
        {
            HandleInput();
            Position = Raymath.Vector2Lerp(Position, TargetPosition, 0.5f);
        }

        private void HandleInput()
        {
            var target = TargetPosition;
            if (Raylib.IsKeyPressed(KeyboardKey.Right)) target.X += tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Left))  target.X -= tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Up))    target.Y -= tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Down))  target.Y += tSize;

            if (target != TargetPosition && IsTileSolid != null && !IsTileSolid(target)) {
                TargetPosition = target;
                OnMoved?.Invoke();
            }
        }

        public bool HasDoorUnlocked(int doorID) => DoorsUnlocked.Contains(doorID);
    }
}