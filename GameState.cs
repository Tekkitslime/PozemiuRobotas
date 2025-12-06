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

    public class ExitState(uint GID, int X, int Y) {
        public uint GID = GID;
        public int X = X, Y = Y;
    }

    public class PeakState(uint GID, int X, int Y, int frameCount) {
        public uint GID = GID;
        public int X = X, Y = Y;
        public int frameCount = frameCount;
        public int frame = 0;
        public readonly float timerTime = 0.1f;
        public float timer = 0;
    }


    public enum GamePlayState {
        Play,
        Dead,
        Win,
    }

    public class GameState {
        public readonly int tileSize = 16;
        public readonly int tilesSeen = 10;

        public Loader loader = null!;
        public Map levelMap = null!;
        public Dictionary<Tileset, List<Texture2D>> tilesetTextures = null!;
        public RenderTexture2D lightMask;

        public GamePlayState gamePlayState = GamePlayState.Play;

        public List<TorchState> torches = null!;
        public List<DoorState> doors = null!;
        public List<KeyState> keys = null!;
        public List<PeakState> peaks = null!;
        public ExitState exit = null!;

        public Vector2 spawn;
        public PlayerState playerState = null!;

        public GameState(string levelTMX)
        {
            loader = Loader.DefaultWith(resourceReader: new ResourceLoader());
            levelMap = loader.LoadMap(levelTMX);
            lightMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());


            var TMXDir = Path.GetDirectoryName(levelTMX)!;
            LoadTilesetTextures(TMXDir);

            LoadMapState();
        }

        public void LoadMapState()
        {
            torches = [];
            doors = [];
            keys = [];
            peaks = [];
            var objectLayer = LayerByName<ObjectLayer>("objects")!;
            var tileObjects = objectLayer.Objects.OfType<TileObject>();

            ParseTorches(tileObjects);
            ParseDoors(tileObjects);
            ParseKeys(tileObjects);
            ParseExit(tileObjects);
            ParsePeaks(tileObjects);
            ParseSpawn(objectLayer.Objects);
            ParsePlayer(tileObjects);
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

        private void ParseExit(IEnumerable<TileObject> tileObjects) {
            var exitObject = tileObjects.Single(tobj => tobj.Type == "Exit");
            exit = new ExitState(exitObject.GID, (int)exitObject.X, (int)exitObject.Y);
        }

        private void ParsePeaks(IEnumerable<TileObject> tileObjects) {
            var peakObjects = tileObjects.Where(tobj => tobj.Type == "Peak");
            foreach (var peak in peakObjects) {
                var tileset = levelMap.ResolveTilesetForGlobalTileID(peak.GID, out var localTileID);
                peaks.Add( new PeakState(peak.GID, (int)peak.X, (int)peak.Y, tileset.TileCount) );
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
            tilesetTextures = [];
            var loader = new ResourceLoader();
            foreach (var tileset in levelMap.Tilesets) {
                if (tileset.Image.HasValue) {
                    var imageName = tileset.Image.Value.Source.Value;
                    var path = Path.Combine(TMXDir, imageName);
                    var textures = new List<Texture2D>();
                    
                    var texture = loader.AdHocLoadTexutre(path);
                    textures.Add(texture);
                    tilesetTextures[tileset] = textures;
                } else {
                    if (tileset.TileCount > 0) {
                        var textures = new List<Texture2D>();

                        foreach (var tile in tileset.Tiles) {
                            var imageName = tile.Image.Value.Source.Value;
                            var path = Path.Combine(TMXDir, imageName);
                            textures.Add(loader.AdHocLoadTexutre(path));
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