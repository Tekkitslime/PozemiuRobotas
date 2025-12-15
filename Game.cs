using System.Numerics;
using DotTiled;
using DotTiled.Serialization;
using Raylib_cs;

namespace PozemiuRobotas {
    public enum GamePlayState {
        Play,
        Dead,
        Win,
    }

    public class Game {
        private readonly float tileSize = 16;
        private readonly float tilesSeen = 10;
        private readonly string levelTMX;

        private Loader loader = null!;
        private Map levelMap = null!;
        private Dictionary<Tileset, List<Texture2D>> tilesetTextures = [];
        private RenderTexture2D lightMask;

        private GamePlayState gamePlayState = GamePlayState.Play;
        private TheWorld theWorld = null!;

        public Game(string levelTMX, IResourceReader? resourceReader = null)
        {
            this.levelTMX = levelTMX;
            this.loader = Loader.DefaultWith(resourceReader: resourceReader);
            this.levelMap = loader.LoadMap(levelTMX);
            this.lightMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }

        public void LoadTheWorld()
        {
            theWorld = new(levelMap, tilesetTextures, tileSize, tilesSeen);
            theWorld.OnExitEntered = () => gamePlayState = GamePlayState.Win;
            theWorld.OnPeakEntered = () => gamePlayState = GamePlayState.Dead;
        }

        public void Draw() {
            switch (gamePlayState)
            {
                case GamePlayState.Play: {
                    Raylib.BeginMode2D(theWorld.camera);
                    theWorld.Draw();
                    Raylib.EndMode2D();

                    PostProcess();

                    break;
                }

                case GamePlayState.Dead: {
                    Raylib.ClearBackground(Color.DarkGray);
                    var width = Raylib.GetScreenWidth()/2;
                    var height = Raylib.GetScreenHeight()/2;
                    Raylib.DrawText("YOU DIED!", width, height, 24, Color.RayWhite);
                    Raylib.DrawText("[Press space to restart]", width, height + 24, 24, Color.RayWhite);
                    break;
                }

                case GamePlayState.Win: {
                    Raylib.ClearBackground(Color.DarkGreen);
                    var width = Raylib.GetScreenWidth()/2;
                    var height = Raylib.GetScreenHeight()/2;
                    Raylib.DrawText("YOU WIN!", width, height, 24, Color.RayWhite);
                    Raylib.DrawText("[Press space to restart]", width, height + 24, 24, Color.RayWhite);
                    break;
                }

            }
        }

        public void Update(float dt) {
            switch (gamePlayState)
            {
                case GamePlayState.Play: {
                    theWorld.Update(dt);
                    break;
                }
                case GamePlayState.Dead:
                case GamePlayState.Win: {
                    if (Raylib.IsKeyPressed(KeyboardKey.Space)) {
                        LoadTheWorld();
                        gamePlayState = GamePlayState.Play;
                    }

                    break;
                }
            }
        }


        public void PostProcess() {
            Raylib.BeginTextureMode(lightMask);
            Raylib.BeginMode2D(theWorld.camera);
            Raylib.ClearBackground(Color.Black);

            // NOTE: Could use this for a nicer looking light
            // Raylib.SetShapesTexture

            Raylib.BeginBlendMode(BlendMode.Additive);

            var halfTile = tileSize/2;
            Raylib.DrawCircleV(
                Raymath.Vector2AddValue(theWorld.player.Position, halfTile),
                radius:  tileSize*2,
                color:   new Color(255, 255, 255, 150)
            );
            Raylib.DrawCircleV(
                Raymath.Vector2AddValue(theWorld.player.Position, halfTile),
                radius:  tileSize,
                color:   new Color(255, 255, 255, 50)
            );

            foreach (var GLEntity in theWorld.gameLoopObjects)
            {
                if (GLEntity is Torch t)
                    Raylib.DrawCircleV(
                        Raymath.Vector2AddValue(t.Position, halfTile),
                        tileSize+Raylib.GetRandomValue(-2, 2),
                        new Color(255, 255, 255, 150)
                    );
            }

            Raylib.EndBlendMode();
            Raylib.EndMode2D();
            Raylib.EndTextureMode();

            Raylib.BeginBlendMode(BlendMode.Multiplied);

            var rect = new Rectangle(Vector2.Zero, lightMask.Texture.Dimensions);
            Raylib.DrawTextureRec(
                lightMask.Texture,
                Util.FlipRectVertical(rect), // Y-flip needed for RenderTextures
                Vector2.Zero,
                Color.White
            );
            
            Raylib.EndBlendMode();
        }

        public void LoadTilesetTextures() {
            var TMXDir = Path.GetDirectoryName(levelTMX)!;

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

        public TheWorld GetTheWorld() => theWorld;
    }
}