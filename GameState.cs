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
        public readonly float tileSize = 16;
        public readonly float tilesSeen = 10;

        public Loader loader = null!;
        public Map levelMap = null!;
        public Dictionary<Tileset, List<Texture2D>> tilesetTextures = null!;
        public RenderTexture2D lightMask;

        public GamePlayState gamePlayState = GamePlayState.Play;
        public TheWorld theWorld = null!;

        public Game(string levelTMX)
        {
            loader = Loader.DefaultWith(resourceReader: new ResourceLoader());
            levelMap = loader.LoadMap(levelTMX);
            lightMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            var TMXDir = Path.GetDirectoryName(levelTMX)!;
            LoadTilesetTextures(TMXDir);
            LoadTheWorld();
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

        public void Update() {
            switch (gamePlayState)
            {
                case GamePlayState.Play: {
                    theWorld.Update(Raylib.GetFrameTime());
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
                Raymath.Vector2AddValue(theWorld.player.position, halfTile),
                radius:  tileSize*2,
                color:   new Color(255, 255, 255, 150)
            );
            Raylib.DrawCircleV(
                Raymath.Vector2AddValue(theWorld.player.position, halfTile),
                radius:  tileSize,
                color:   new Color(255, 255, 255, 50)
            );

            foreach (var torch in theWorld.torches) {
                Raylib.DrawCircleV(
                    Raymath.Vector2AddValue(torch.position, halfTile),
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