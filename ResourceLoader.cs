using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using DotTiled.Serialization;
using Raylib_cs;

namespace PozemiuRobotas {
    public class ResourceLoader : IResourceReader
    {
        public string Read(string resourcePath)
        {
            Console.WriteLine($"loading (base): {resourcePath}");
            resourcePath = "PozemiuRobotas." + resourcePath.Replace('/', '.');
            Console.WriteLine($"loading: {resourcePath}");
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourcePath)!;
            using var reader = new StreamReader(stream)!;

            return reader.ReadToEnd();
        }

        public Texture2D AdHocLoadTexutre(string imagePath) {
            Console.WriteLine($"adhoc loading (base): {imagePath}");
            imagePath = "res/" + imagePath.Substring("res/levels/../".Length);
            imagePath = "PozemiuRobotas." + imagePath.Replace('/', '.');
            Console.WriteLine($"adhoc loading: {imagePath}");
            
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(imagePath)!;
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);

            var bytes = memStream.ToArray();
            
            var image = Raylib.LoadImageFromMemory(".png", bytes);
            var texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);

            return texture;
        }
    }
}