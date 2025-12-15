using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas
{
    public interface IGameLoopObject
    {
        public void Draw();
        public void Update(float dt);
        public bool IsMarkedForRemoval { get; set; }
    }

    public abstract class GameEntity(TileObject tobj, Texture2D? Texture, Rectangle? SrcRect) : IGameLoopObject
    {
        public Vector2 Position { get; set; } = new(tobj.X, tobj.Y);
        public Texture2D? Texture { get; set; } = Texture;
        public Rectangle? SrcRect { get; set; } = SrcRect;

        public bool Visible { get; set; } = tobj.Visible;
        public FlippingFlags FlippingFlags { get; set; } = tobj.FlippingFlags;

        public bool IsMarkedForRemoval { get; set; } = false;

        public virtual void Draw()
        {
            if (!Visible || Texture == null || SrcRect == null) return;
            var sourceRectangle = Util.FlipRect(FlippingFlags, (Rectangle)SrcRect!);

            Raylib.DrawTextureRec((Texture2D)Texture!, sourceRectangle, Position, Color.White);
        }

        public virtual void Update(float dt)
        {

        }
    }

    public abstract class AnimatedGameEntity(TileObject tobj, List<Texture2D> Textures) : IGameLoopObject
    {
        public Vector2 Position { get; set; } = new(tobj.X, tobj.Y);
        public List<Texture2D> Textures { get; set { Textures = value; this.FrameCount = value.Count; } } = Textures;

        public bool Visible { get; set; } = tobj.Visible;
        public FlippingFlags FlippingFlags { get; set; } = tobj.FlippingFlags;

        public int FrameCount { get; set; } = Textures?.Count ?? 0;
        public int Frame { get; set; } = 0;
        public float TimerTime { get; set; } = 0.1f;
        public float Timer { get; set; } = 0;

        public bool IsMarkedForRemoval { get; set; } = false;

        public virtual void Draw()
        {
            if (!Visible || (Textures?.Count ?? 0) == 0) return;
            var texture = Textures?[Frame];
            var sourceRectangle = Util.FlipRect(FlippingFlags, new Rectangle(Vector2.Zero, texture?.Dimensions ?? Vector2.Zero));

            if (texture == null) return;
            Raylib.DrawTextureRec((Texture2D)texture, sourceRectangle, Position, Color.White);
        }

        public virtual void Update(float dt) {
            if (FrameCount == 0) return;
            
            Timer -= dt;
            if (Timer < 0)
            {
                Timer = TimerTime;
                Frame = (Frame+1)%FrameCount;
            }
        }
    }

}