using System.Numerics;
using DotTiled.Serialization;
using Raylib_cs;

namespace PozemiuRobotas {
    public class PozemiuRobotas {
        public static void Main(string[] _) {
            Raylib.InitWindow(800, 600, "Požemių robotas");
            Raylib.SetTargetFPS(24);

            var gameState = new GameState("res/levels/level1.tmx");

            var background = new Color(0x25, 0x13, 0x1a, 0xff);
            while (!Raylib.WindowShouldClose()) {
                Raylib.BeginDrawing();
                Raylib.BeginMode2D(gameState.playerState.camera);
                Raylib.ClearBackground(background);

                // Draw
                TheWorld.DrawTileLayer(gameState, "world");
                TheWorld.DrawTileLayer(gameState, "decor");
                TheWorld.DrawTorches(gameState);
                TheWorld.DrawDoors(gameState);
                TheWorld.DrawKeys(gameState);
                Player.Draw(gameState);

                TheWorld.PostProcess(gameState);
                Raylib.EndMode2D();
                Raylib.EndDrawing();

                // Update
                Player.Update(gameState);
                TheWorld.Update(gameState);
            }

            Raylib.CloseWindow();
        }


    }
}