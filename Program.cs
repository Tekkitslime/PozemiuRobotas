using System.Numerics;
using System.Reflection;
using DotTiled.Serialization;
using Raylib_cs;

namespace PozemiuRobotas {
    public class PozemiuRobotas {
        public static void Main(string[] _) {
            Raylib.InitWindow(800, 600, "Požemių robotas");
            Raylib.SetTargetFPS(24);

            var game = new Game("res/levels/level1.tmx");

            var background = new Color(0x25, 0x13, 0x1a, 0xff);
            while (!Raylib.WindowShouldClose()) {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(background);

                game.Draw();

                Raylib.EndDrawing();

                game.Update();
            }

            Raylib.CloseWindow();
        }


    }
}