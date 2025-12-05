using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas {
    public class Player {
        public static void Update(GameState gameState)
        {
            var pState = gameState.playerState;


            HandleInput(gameState);
            pState.Position = Raymath.Vector2Lerp(pState.Position, pState.TargetPosition, 0.5f);

            UpdateCameraTarget(gameState);
        }

        private static void HandleInput(GameState gameState)
        {
            var pState = gameState.playerState;
            var target = pState.TargetPosition;
            var tSize = gameState.tileSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Right)) target.X += tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Left)) target.X -= tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Up)) target.Y -= tSize;
            if (Raylib.IsKeyPressed(KeyboardKey.Down)) target.Y += tSize;

            if (target != pState.TargetPosition) {
                // Console.WriteLine(String.Format("{0}, {1}", pState.CurDoorOpened, pState.LastDoorOpened));
                if (!TheWorld.IsTileSolid(gameState, target))
                {
                    pState.TargetPosition = target;
                }

                var key = Key.IsOnKey(gameState, pState.TargetPosition);
                if (key != null) {
                    CollectKey(gameState, key);
                }
            }

        }

        private static void CollectKey(GameState gameState, KeyState key) {
            var pState = gameState.playerState;
            pState.DoorsUnlocked.Add(key.doorID);
            gameState.keys.Remove(key);
        }

        private static void UpdateCameraTarget(GameState gameState)
        {
            var pState = gameState.playerState;
            var pos = pState.Position;
            pos.X += gameState.tileSize / 2;
            pos.Y += gameState.tileSize / 2;
            pState.camera.Target = pos;
        }

        public static void Draw(GameState gameState) {
            var player = gameState.playerState;
            var GID = player.GID;
            var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(GID, out var localTileID);
            var tileSrcRect = tileset.GetSourceRectangleForLocalTileID(localTileID);
            var texture = gameState.tilesetTextures[tileset][0];

            var srcRect = new Rectangle(tileSrcRect.X, tileSrcRect.Y, tileSrcRect.Width, tileSrcRect.Height);
            var dstRect = srcRect with { X = player.Position.X, Y = player.Position.Y };
            Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(0, 0), 0, Color.White);
        }

        public static void LoadCamera(GameState gameState) {
            var center = new Vector2(Raylib.GetScreenWidth()/2, Raylib.GetScreenHeight()/2);
            var zoom = Raylib.GetScreenWidth()/(gameState.tileSize*gameState.tilesSeen);
            gameState.playerState.camera = new Camera2D(offset: center, target: Vector2.Zero, rotation: 0, zoom);
        }
    }

    public class Key {
        public static KeyState? IsOnKey(GameState gameState, Vector2 tileCoord) {
            foreach (var key in gameState.keys) {
                if (tileCoord == new Vector2(key.X, key.Y)) return key;
            }
            return null;
        }
    }

    public class Door {
        public static bool IsUnlocked(GameState gameState, int doorID) {
            var pState = gameState.playerState;
            return pState.DoorsUnlocked.Any(id => id == doorID);
        }

        public static void ToggleDoor(GameState gameState, int doorID) {
            var doors = gameState.doors.Where(d => d.doorID == doorID);
            foreach (var door in doors) {
                door.visible = !door.visible;
            }
        }
    }

    public class TheWorld {
        public static void Update(GameState gameState) {
            var pState = gameState.playerState;

            if (pState.CurDoorOpened  != 0) Door.ToggleDoor(gameState, pState.CurDoorOpened);
            if (pState.LastDoorOpened != 0) Door.ToggleDoor(gameState, pState.LastDoorOpened);
            pState.LastDoorOpened = pState.CurDoorOpened;
        }

        public static void DrawObjectLayer(GameState gameState, string layerName) {
            var layer = gameState.LayerByName<ObjectLayer>(layerName)!;
            DrawObjectLayer(gameState, layer);
        }

        public static void DrawObjectLayer(GameState gameState, ObjectLayer layer) {

            foreach (var obj in layer.Objects) {
                if (!obj.Visible) continue;

                if (obj is TileObject tobj) {
                    var GID = tobj.GID;
                    var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(GID, out var localTileID);
                    var tileSrcRect = tileset.GetSourceRectangleForLocalTileID(localTileID);
                    var texture = gameState.tilesetTextures[tileset][0];

                    var srcRect = new Rectangle(tileSrcRect.X, tileSrcRect.Y, tileSrcRect.Width, tileSrcRect.Height);
                    var dstRect = srcRect with { X = tobj.X, Y = tobj.Y };
                    Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(0, 0), 0, Color.White);
                }
            }
        }


        public static void DrawTileLayer(GameState gameState, string layerName) {
            var layer = gameState.LayerByName<TileLayer>(layerName)!;
            DrawTileLayer(gameState, layer);
        }

        public static void DrawTileLayer(GameState gameState, TileLayer layer) {

            for (int y=0; y < layer.Height; y++) {
                for (int x=0; x < layer.Width; x++) {
                    var id = layer.GetGlobalTileIDAtCoord(x, y);
                    if (id == 0) continue;

                    var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(id, out var localTileID);
                    var r = tileset.GetSourceRectangleForLocalTileID(localTileID);

                    var srcRect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                    var dstRect = srcRect with { X = x*r.Width, Y = y*r.Height };

                    var texture = gameState.tilesetTextures[tileset][0];
                    Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(), 0, Color.White);
                }
            }
        }

        public static void DrawTorches(GameState gameState) {
            foreach (var torch in gameState.torches)
            {
                var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(torch.GID, out var localTileID);

                var r = tileset.GetSourceRectangleForLocalTileID(localTileID);
                var srcRect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                srcRect = FlipRect(torch.FFlags, srcRect);

                var dstRect = new Rectangle(torch.X, torch.Y, r.Width, r.Height);

                var texture = gameState.tilesetTextures[tileset][0];
                Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(), 0, Color.White);
            }
        }

        public static void DrawDoors(GameState gameState) {
            foreach (var door in gameState.doors)
            {
                if (!door.visible) continue;
                var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(door.GID, out var localTileID);

                var r = tileset.GetSourceRectangleForLocalTileID(localTileID);
                var srcRect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                var dstRect = new Rectangle(door.X, door.Y, r.Width, r.Height);

                var texture = gameState.tilesetTextures[tileset][0];
                Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(), 0, Color.White);
            }
        }

        public static void DrawKeys(GameState gameState) {
            foreach (var key in gameState.keys)
            {
                var tileset = gameState.levelMap.ResolveTilesetForGlobalTileID(key.GID, out var localTileID);

                var r = tileset.GetSourceRectangleForLocalTileID(localTileID);
                var srcRect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                var dstRect = new Rectangle(key.X, key.Y, r.Width, r.Height);

                var texture = gameState.tilesetTextures[tileset][0];
                Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(), 0, Color.White);
            }
        }

        private static Rectangle FlipRect(FlippingFlags FFlags, Rectangle srcRect)
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

        public static void PostProcess(GameState gameState) {
            Raylib.BeginTextureMode(gameState.lightMask);
            Raylib.BeginMode2D(gameState.playerState.camera);
            Raylib.ClearBackground(new Color(0, 0, 0, 200));

            // NOTE: Could use this for a nicer looking light
            // Raylib.SetShapesTexture

            Raylib.BeginBlendMode(BlendMode.Additive);

            var pState = gameState.playerState;
            var halfTile = gameState.tileSize/2;
            Raylib.DrawCircle(
                centerX: (int)pState.Position.X + halfTile,
                centerY: (int)pState.Position.Y + halfTile,
                radius:  gameState.tileSize*2,
                color:   new Color(255, 255, 255, 150)
            );
            Raylib.DrawCircle(
                centerX: (int)pState.Position.X + halfTile,
                centerY: (int)pState.Position.Y + halfTile,
                radius:  gameState.tileSize,
                color:   new Color(255, 255, 255, 50)
            );

            foreach (var torch in gameState.torches) {
                Raylib.DrawCircle(
                    torch.X + halfTile,
                    torch.Y + halfTile,
                    gameState.tileSize+Raylib.GetRandomValue(-2, 2),
                    new Color(255, 255, 255, 150)
                );
            }

            Raylib.EndBlendMode();
            Raylib.EndMode2D();
            Raylib.EndTextureMode();

            Raylib.BeginBlendMode(BlendMode.Multiplied);
            var lightMask = gameState.lightMask;
            Raylib.DrawTexturePro(
                lightMask.Texture,
                new Rectangle(0, 0, lightMask.Texture.Width, -lightMask.Texture.Height), // Y-flip needed for RenderTextures
                new Rectangle(0, 0, lightMask.Texture.Width, lightMask.Texture.Height),
                Vector2.Zero,
                0.0f,
                Color.White
            );
            
            Raylib.EndBlendMode();
        }

        public static bool IsTileSolid(GameState gameState, Vector2 tileCoordinate) {
            var collisionLayer = gameState.LayerByName<ObjectLayer>("collision")!;

            foreach (var collider in collisionLayer.Objects) {
                var rect = new Rectangle(collider.X, collider.Y, collider.Width, collider.Height);
                if (Raylib.CheckCollisionPointRec(tileCoordinate, rect)) return true;
            }

            gameState.playerState.CurDoorOpened = 0;
            foreach (var door in gameState.doors) {
                if (tileCoordinate.X == door.X && tileCoordinate.Y == door.Y) {
                    if (Door.IsUnlocked(gameState, door.doorID)) {
                        gameState.playerState.CurDoorOpened = door.doorID;
                    } else {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}