using System.Numerics;
using DotTiled;
using Raylib_cs;

namespace PozemiuRobotas {
    public class TheWorld {
        private readonly Map map;
        private readonly Dictionary<Tileset, List<Texture2D>> tilesetTextures;
        private readonly float tileSize;
        private readonly float tileSizeHalf;
        private readonly float tilesSeen;

        public List<IGameLoopObject> gameLoopObjects;

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

            gameLoopObjects = [];

            collisionLayer = map.Layers.OfType<ObjectLayer>().Single(l => l.Name == "collision");
            var objectLayer = map.Layers.OfType<ObjectLayer>().Single(l => l.Name == "objects");
            var tileObjects = objectLayer.Objects.OfType<TileObject>();

            gameLoopObjects.AddRange(ParseGameLoopObjects(map, "objects"));

            var center = new Vector2(Raylib.GetScreenWidth()/2, Raylib.GetScreenHeight()/2);
            var zoom = Raylib.GetScreenWidth()/(tileSize*tilesSeen);
            this.camera = new(
                offset: center,
                target: Raymath.Vector2AddValue(player.Position, tileSizeHalf),
                rotation: 0, zoom
            );
        }

        private List<IGameLoopObject> ParseGameLoopObjects(Map map, string layerName)
        {
            var tileLayer = map.Layers.OfType<ObjectLayer>().Single(l => l.Name == layerName);
            var tileObjects = tileLayer.Objects.OfType<TileObject>().ToArray();
            List<IGameLoopObject> gameLoopObjects = [];

            foreach (var tobj in tileObjects)
            {
                var tileset = map.ResolveTilesetForGlobalTileID(tobj.GID, out var localTileID);
                var sr = tileset.GetSourceRectangleForLocalTileID(localTileID);
                var srcRect = Util.DotTiledRectToRaylibRect(sr);
                var textures = tilesetTextures[tileset];

                switch (tobj.Type)
                {
                    case "Torch": {
                        gameLoopObjects.Add(new Torch(tobj, textures[0], srcRect));
                        break;
                    }
                    case "Key": {
                        gameLoopObjects.Add(new Key(tobj, textures[0], srcRect));
                        break;
                    }
                    case "Door": {
                        gameLoopObjects.Add(new Door(tobj, textures[0], srcRect));
                        break;
                    }
                    case "Peak": {
                        gameLoopObjects.Add(new Peak(tobj, textures));
                        break;
                    }
                    case "Player": {
                        this.player = new(tobj, textures[0], srcRect)
                            {
                                OnMoved = this.OnMoved,
                                IsTileSolid = this.IsTileSolid
                            };
                        gameLoopObjects.Add(this.player);

                        break;
                    }
                    case "Exit": {
                        this.exit = new(tobj, textures[0], srcRect);
                        gameLoopObjects.Add(this.exit);
                        break;
                    }
                    default: {
                        throw new Exception($"Unknown object type: {tobj.Type} in layer '{layerName}'");
                    }
                }
            }

            return gameLoopObjects;
        }

        private void OnMoved()
        {
            // close all open doors
            var doors = gameLoopObjects.Where(obj => obj is Door).Cast<Door>();
            foreach (var door in doors)
            {
                if (door.isOpen) {
                    door.Visible = !door.Visible;
                    door.isOpen = false;
                }
            }
            // open active door
            var doorGroupToOpen = doors.FirstOrDefault(door => door.Position == player.TargetPosition)?.doorID;
            if (doorGroupToOpen != null) {
                foreach (var door in doors)
                {
                    if (door.doorID == doorGroupToOpen)
                    {
                        door.Visible = !door.Visible;
                        door.isOpen = true;
                    }
                }
            }

            // collect key
            for (int i = 0; i < gameLoopObjects.Count; i++)
            {
                if (gameLoopObjects[i] is Key key)
                    if (player.TargetPosition == key.Position)
                    {
                        player.DoorsUnlocked.Add(key.doorID);
                        key.IsMarkedForRemoval = true;
                        break;
                    }
            }

            // die on peak
            for (int i = 0; i < gameLoopObjects.Count; i++)
            {
                if (gameLoopObjects[i] is Peak peak)
                    if (player.TargetPosition == peak.Position)
                        OnPeakEntered?.Invoke();
            }

            // exit on exit
            if (player.TargetPosition == exit.Position)
                OnExitEntered?.Invoke();
        }

        public void Update(float dt)
        {
            camera.Target = Raymath.Vector2AddValue(player.Position, tileSizeHalf);
            foreach (var GLEntity in gameLoopObjects) GLEntity.Update(dt);
            for (int i=gameLoopObjects.Count-1; i>=0; i--)
            {
                if (gameLoopObjects[i].IsMarkedForRemoval)
                    gameLoopObjects.RemoveAt(i);
            }
        }

        public void Draw()
        {
            DrawTileLayer("world");
            DrawTileLayer("decor");
            foreach (var GLEntity in gameLoopObjects) GLEntity.Draw();
        }

        private void DrawTileLayer(string layerName) {
            var layer = map.Layers.OfType<TileLayer>().Single(l => l.Name == layerName);
            DrawTileLayer(layer);
        }

        private void DrawTileLayer(TileLayer layer) {
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

            var doors = gameLoopObjects.Where(obj => obj is Door).Cast<Door>();
            foreach (var door in doors) {
                if (door.Position == tileCoordinate) {
                    if (!player.HasDoorUnlocked(door.doorID)) return true;
                }
            }

            return false;
        }
    }
}