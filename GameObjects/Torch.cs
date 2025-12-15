using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas
{
    public class Torch(TileObject tobj, Texture2D? texture, Rectangle? srcRect) : GameEntity(tobj, texture, srcRect)
    {
    }
}