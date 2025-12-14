using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas {
    public class Player(TileObject tobj, Texture2D texture, Rectangle srcRect) {
        private readonly int tSize = 16;
        public uint GID = tobj.GID;
        public Vector2 position = new(tobj.X, tobj.Y);
        public Texture2D texture = texture;
        public Rectangle srcRect = srcRect;

        public Action? OnMoved;
        public Func<Vector2, bool>? IsTileSolid;
        
        public List<int> DoorsUnlocked = [];

        public Vector2 TargetPosition = new(tobj.X, tobj.Y);
        public int standingOnDoorGroup = 0;

        public void Update(float dt)
        {
            HandleInput();
            position = Raymath.Vector2Lerp(position, TargetPosition, 0.5f);
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

        public void Draw() {
            Raylib.DrawTextureRec(texture, srcRect, position, Color.White);
        }
    }

    public class Torch(TileObject tobj, Texture2D texture, Rectangle srcRect) {
        public uint GID = tobj.GID;
        public Vector2 position = new(tobj.X, tobj.Y);
        public Texture2D texture = texture;
        public Rectangle srcRect = srcRect;
        public FlippingFlags flippingFlags = tobj.FlippingFlags;

        public void Draw() {
            var srcRect = Util.FlipRect(flippingFlags, this.srcRect);
            Raylib.DrawTextureRec(texture, srcRect, position, Color.White);
        }
    }

    public class Exit(TileObject tobj, Texture2D texture, Rectangle srcRect) {
        public uint GID = tobj.GID;
        public Vector2 position = new(tobj.X, tobj.Y);
        public Texture2D texture = texture;

        public void Draw() {
            Raylib.DrawTextureRec(texture, srcRect, position, Color.White);
        }
    }

    public class Key(TileObject tobj, Texture2D texture, Rectangle srcRect) {
        public uint GID = tobj.GID;
        public Vector2 position = new(tobj.X, tobj.Y);
        public Texture2D texture = texture;
        public Rectangle srcRect = srcRect;
        public int doorID = tobj.GetProperty<IntProperty>("door").Value;

        public void Draw() {
            Raylib.DrawTextureRec(texture, srcRect, position, Color.White);
        }
    }

    public class Door(TileObject tobj, Texture2D texture, Rectangle srcRect)
    {
        public uint GID = tobj.GID;
        public bool visible = tobj.Visible;
        public Vector2 position = new(tobj.X, tobj.Y);
        public int doorID = tobj.GetProperty<IntProperty>("door").Value;
        public bool isOpen = false;

        public Texture2D texture = texture;
        public Rectangle srcRect = srcRect;

        public void Draw() {
            if (!visible) return;
            Raylib.DrawTextureRec(texture, srcRect, position, Color.White);
        }
    }

    public class Peak(TileObject tobj, List<Texture2D> textures) {
        public uint GID = tobj.GID;
        public Vector2 position = new(tobj.X, tobj.Y);

        public int frameCount = textures.Count;
        public int frame = 0;
        public readonly float timerTime = 0.1f;
        public float timer = 0;

        public void Update(float dt) {
            timer -= dt;
            if (timer < 0)
            {
                timer = timerTime;
                frame = (frame+1)%frameCount;
            }
        }

        public void Draw() {
            var texture = textures[frame];
            Raylib.DrawTextureV(texture, position, Color.White);
        }
    }

    public class TheWorld {
        private readonly Map map;
        private readonly Dictionary<Tileset, List<Texture2D>> tilesetTextures;
        private readonly float tileSize;
        private readonly float tileSizeHalf;
        private readonly float tilesSeen;

        public List<Torch> torches;
        public List<Door> doors;
        public List<Key> keys;
        public List<Peak> peaks;

        public Action? OnPeakEntered;
        public Action? OnExitEntered;

        public ObjectLayer collisionLayer;

        public Camera2D camera;
        public Player player = null!;
        public Exit exit = null!;

        public TheWorld(Map map, Dictionary<Tileset, List<Texture2D>> tilesetTextures, float tileSize, float tilesSeen)
        {
            this.map = map;
            this.tilesetTextures = tilesetTextures;
            this.tileSize = tileSize;
            this.tileSizeHalf = tileSize/2.0f;
            this.tilesSeen = tilesSeen;

            torches = [];
            doors = [];
            keys = [];
            peaks = [];

            collisionLayer = map.Layers.OfType<ObjectLayer>().Single(l => l.Name == "collision");
            var objectLayer = map.Layers.OfType<ObjectLayer>().Single(l => l.Name == "objects");
            var tileObjects = objectLayer.Objects.OfType<TileObject>();

            ParseTorches(tileObjects);
            ParseDoors(tileObjects);
            ParseKeys(tileObjects);
            ParseExit(tileObjects);
            ParsePeaks(tileObjects);
            ParsePlayer(tileObjects);

            var center = new Vector2(Raylib.GetScreenWidth()/2, Raylib.GetScreenHeight()/2);
            var zoom = Raylib.GetScreenWidth()/(tileSize*tilesSeen);
            this.camera = new(
                offset: center,
                target: Raymath.Vector2AddValue(player.position, tileSizeHalf),
                rotation: 0, zoom
            );
        }

        private void ParseTorches(IEnumerable<TileObject> tileObjects) {
            var torchObjects = tileObjects.Where(tobj => tobj.Type == "Torch");
            foreach (var torch in torchObjects) {
                var tileset = map.ResolveTilesetForGlobalTileID(torch.GID, out var localTileID);
                var rect = tileset.GetSourceRectangleForLocalTileID(localTileID);

                torches.Add(new(torch, tilesetTextures[tileset][0], Util.DotTiledRectToRaylibRect(rect)));
            }
        }

        private void ParseDoors(IEnumerable<TileObject> tileObjects) {
            var doorObjects = tileObjects.Where(tobj => tobj.Type == "Door");
            foreach (var door in doorObjects) {
                var tileset = map.ResolveTilesetForGlobalTileID(door.GID, out var localTileID);
                var rect = tileset.GetSourceRectangleForLocalTileID(localTileID);

                doors.Add(new(door, tilesetTextures[tileset][0], Util.DotTiledRectToRaylibRect(rect)));
            }
        }

        private void ParseKeys(IEnumerable<TileObject> tileObjects) {
            var keyObjects = tileObjects.Where(tobj => tobj.Type == "Key");
            foreach (var key in keyObjects) {
                var tileset = map.ResolveTilesetForGlobalTileID(key.GID, out var localTileID);
                var rect = tileset.GetSourceRectangleForLocalTileID(localTileID);

                keys.Add( new(key, tilesetTextures[tileset][0], Util.DotTiledRectToRaylibRect(rect)) );
            }
        }

        private void ParseExit(IEnumerable<TileObject> tileObjects) {
            var exitObject = tileObjects.Single(tobj => tobj.Type == "Exit");
            var tileset = map.ResolveTilesetForGlobalTileID(exitObject.GID, out var localTileID);
            var rect = tileset.GetSourceRectangleForLocalTileID(localTileID);

            exit = new(exitObject, tilesetTextures[tileset][0], Util.DotTiledRectToRaylibRect(rect));
        }

        private void ParsePeaks(IEnumerable<TileObject> tileObjects) {
            var peakObjects = tileObjects.Where(tobj => tobj.Type == "Peak");
            foreach (var peak in peakObjects) {
                var tileset = map.ResolveTilesetForGlobalTileID(peak.GID, out var localTileID);
                peaks.Add( new(peak, tilesetTextures[tileset]) );
            }
        }

        private void ParsePlayer(IEnumerable<TileObject> tileObjects) {
            var playerObject = tileObjects.Single(tobj => tobj.Type == "Player");
            var tileset = map.ResolveTilesetForGlobalTileID(playerObject.GID, out var localTileID);
            var rect = tileset.GetSourceRectangleForLocalTileID(localTileID);

            player = new(playerObject, tilesetTextures[tileset][0], Util.DotTiledRectToRaylibRect(rect))
            {
                OnMoved = this.OnMoved,
                IsTileSolid = this.IsTileSolid
            };
        }

        private void OnMoved()
        {
            // close all open doors
            foreach (var door in doors)
            {
                if (door.isOpen) {
                    door.visible = !door.visible;
                    door.isOpen = false;
                }
            }
            // open active door
            var doorGroupToOpen = doors.Find(door => door.position == player.TargetPosition)?.doorID;
            if (doorGroupToOpen != null) {
                foreach (var door in doors)
                {
                    if (door.doorID == doorGroupToOpen)
                    {
                        door.visible = !door.visible;
                        door.isOpen = true;
                    }
                }
            }

            // collect key
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (player.TargetPosition == key.position)
                {
                    player.DoorsUnlocked.Add(key.doorID);
                    keys.RemoveAt(i);
                    break;
                }
            }

            // die on peak
            for (int i = 0; i < peaks.Count; i++)
            {
                var peak = peaks[i];
                if (player.TargetPosition == peak.position)
                    OnPeakEntered?.Invoke();
            }

            // exit on exit
            if (player.TargetPosition == exit.position)
                OnExitEntered?.Invoke();
        }

        public void Update(float dt)
        {
            player.Update(dt);
            camera.Target = Raymath.Vector2AddValue(player.position, tileSizeHalf);
            foreach (var peak in peaks) peak.Update(dt);
        }

        public void Draw()
        {
            DrawTileLayer("world");
            DrawTileLayer("decor");
            foreach (var torch in torches) torch.Draw();
            foreach (var door in doors)    door.Draw();
            foreach (var key in keys)      key.Draw();
            foreach (var peak in peaks)    peak.Draw();
            exit.Draw();
            player.Draw();
        }

        public void DrawTileLayer(string layerName) {
            var layer = map.Layers.OfType<TileLayer>().Single(l => l.Name == layerName);
            DrawTileLayer(layer);
        }

        public void DrawTileLayer(TileLayer layer) {
            for (int y=0; y < layer.Height; y++) {
                for (int x=0; x < layer.Width; x++) {
                    var id = layer.GetGlobalTileIDAtCoord(x, y);
                    if (id == 0) continue;

                    var tileset = map.ResolveTilesetForGlobalTileID(id, out var localTileID);
                    var r = tileset.GetSourceRectangleForLocalTileID(localTileID);

                    var srcRect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                    var dstRect = srcRect with { X = x*r.Width, Y = y*r.Height };

                    var texture = tilesetTextures[tileset][0];
                    Raylib.DrawTexturePro(texture, srcRect, dstRect, new Vector2(), 0, Color.White);
                }
            }
        }

        public bool IsTileSolid(Vector2 tileCoordinate) {
            foreach (var collider in collisionLayer.Objects) {
                var rect = new Rectangle(collider.X, collider.Y, collider.Width, collider.Height);
                if (Raylib.CheckCollisionPointRec(tileCoordinate, rect)) return true;
            }

            player.standingOnDoorGroup = 0;
            foreach (var door in doors) {
                if (door.position == tileCoordinate) {
                    if (!player.HasDoorUnlocked(door.doorID)) return true;
                }
            }

            return false;
        }
    }
}