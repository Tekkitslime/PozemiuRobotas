using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas
{
    public class Key(TileObject tobj, Texture2D? texture, Rectangle? srcRect) : GameEntity(tobj, texture, srcRect)
    {
        public int doorID = tobj.GetProperty<IntProperty>("door").Value;
    }
}