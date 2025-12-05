using System.Numerics;
using DotTiled;
using DotTiled.Serialization;
using Raylib_cs;

namespace PozemiuRobotas {
    public class PlayerState(uint GID, Vector2 Position) {
        public uint GID = GID;
        public Vector2 Position = Position;
        public List<int> DoorsUnlocked = [];
        public Camera2D camera;

        public Vector2 TargetPosition = Position;
        public int LastDoorOpened = 0;
        public int CurDoorOpened = 0;
    }

    public class TorchState(uint GID, int X, int Y, FlippingFlags FFlags) {
        public uint GID = GID;
        public int X = X, Y = Y; 
        public FlippingFlags FFlags = FFlags;
    }

    public class DoorState(uint GID, int doorID, bool visible, int X, int Y) {
        public uint GID = GID;
        public int doorID = doorID;
        public bool visible = visible;
        public int X = X, Y = Y;
    }

    public class KeyState(uint GID, int doorID, int X, int Y) {
        public uint GID = GID;
        public int doorID = doorID;
        public int X = X, Y = Y;
    }

    public class GameState {
        public readonly int tileSize = 16;
        public readonly int tilesSeen = 10;

        public Loader loader;
        public Map levelMap;
        public Dictionary<Tileset, List<Texture2D>> tilesetTextures;
        public RenderTexture2D lightMask;

        public List<TorchState> torches;
        public List<DoorState> doors;
        public List<KeyState> keys;

        public Vector2 spawn;
        public PlayerState playerState = null!;

        public GameState(string levelTMX)
        {
            loader = Loader.Default();
            levelMap = loader.LoadMap(levelTMX);
            tilesetTextures = [];

            torches = [];
            doors = [];
            keys = [];

            var TMXDir = Path.GetDirectoryName(levelTMX)!;
            LoadTilesetTextures(TMXDir);

            var objectLayer = LayerByName<ObjectLayer>("objects")!;
            var tileObjects = objectLayer.Objects.OfType<TileObject>();

            ParseTorches(tileObjects);
            ParseDoors(tileObjects);
            ParseKeys(tileObjects);
            ParseSpawn(objectLayer.Objects);
            ParsePlayer(tileObjects);

            lightMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }

        private void ParseTorches(IEnumerable<TileObject> tileObjects) {
            var torchObjects = tileObjects.Where(tobj => tobj.Type == "Torch");
            foreach (var torch in torchObjects) {
                torches.Add( new TorchState(torch.GID, (int)torch.X, (int)torch.Y, torch.FlippingFlags) );
            }
        }

        private void ParseDoors(IEnumerable<TileObject> tileObjects) {
            var doorObjects = tileObjects.Where(tobj => tobj.Type == "Door");
            foreach (var door in doorObjects) {
                doors.Add( new DoorState(door.GID, door.GetProperty<IntProperty>("door").Value, door.Visible, (int)door.X, (int)door.Y) );
            }
        }

        private void ParseKeys(IEnumerable<TileObject> tileObjects) {
            var keyObjects = tileObjects.Where(tobj => tobj.Type == "Key");
            foreach (var key in keyObjects) {
                keys.Add( new KeyState(key.GID, key.GetProperty<IntProperty>("door").Value, (int)key.X, (int)key.Y) );
            }
        }

        private void ParsePlayer(IEnumerable<TileObject> tileObjects) {
            var playerObject = tileObjects.Single(tobj => tobj.Type == "Player");
            playerState = new PlayerState(playerObject.GID, spawn);
            Player.LoadCamera(this);
        }

        private void ParseSpawn(IEnumerable<DotTiled.Object> objects) {
            var spawnObject = objects.Single(tobj => tobj.Name == "Spawn");
            spawn = new Vector2(spawnObject.X, spawnObject.Y);
        }

        private void LoadTilesetTextures(string TMXDir) {
            foreach (var tileset in levelMap.Tilesets) {
                if (tileset.Image.HasValue) {
                    var imageName = tileset.Image.Value.Source.Value;
                    var path = Path.Combine(TMXDir, imageName);
                    var textures = new List<Texture2D>();
                    
                    var texture = Raylib.LoadTexture(path);
                    textures.Add(texture);
                    tilesetTextures[tileset] = textures;
                } else {
                    if (tileset.TileCount > 0) {
                        var textures = new List<Texture2D>();

                        foreach (var tile in tileset.Tiles) {
                            var imageName = tile.Image.Value.Source.Value;
                            var path = Path.Combine(TMXDir, imageName);
                            textures.Add(Raylib.LoadTexture(path));
                        }

                        tilesetTextures[tileset] = textures;
                    }
                }
            }
        }

        public LayerType? LayerByName<LayerType>(string layerName) where LayerType : BaseLayer {
            return levelMap.Layers.OfType<LayerType>().Single(l => l.Name == layerName);
        }
    }
}