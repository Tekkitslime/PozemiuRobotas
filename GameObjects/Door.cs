using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas
{
    public class Door(TileObject tobj, Texture2D texture, Rectangle srcRect) : GameEntity(tobj, texture, srcRect)
    {
        public int doorID = tobj.GetProperty<IntProperty>("door").Value;
        public bool isOpen = false;
    }

}