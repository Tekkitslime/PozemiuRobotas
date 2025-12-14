using DotTiled;

namespace PozemiuRobotas
{
    public class Util
    {
        public static Raylib_cs.Rectangle DotTiledRectToRaylibRect(DotTiled.SourceRectangle numRect) =>
            new( numRect.X, numRect.Y, numRect.Width, numRect.Height );

        public static Raylib_cs.Rectangle FlipRect(FlippingFlags FFlags, Raylib_cs.Rectangle srcRect)
        {
            if (FFlags.HasFlag(FlippingFlags.FlippedHorizontally))
            {
                // srcRect.X += srcRect.Width;
                srcRect.Width = -srcRect.Width;
            }
            if (FFlags.HasFlag(FlippingFlags.FlippedVertically))
            {
                // srcRect.Y += srcRect.Height;
                srcRect.Height = -srcRect.Height;
            }

            return srcRect;
        }

        public static Raylib_cs.Rectangle FlipRectVertical(Raylib_cs.Rectangle rect)
        {
            return rect with { Height = -rect.Height };
        }


    }
}