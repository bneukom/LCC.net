using System.IO;

namespace LandscapeClassifier.Model
{
    public class WorldFile
    {
        public float PixelSizeX { get; }
        public float RotationY { get;}
        public float RotationX { get; }
        public float PixelSizeY { get; }
        public float X { get; }
        public float Y { get; }
        public readonly string FileName;

        public WorldFile(float pixelSizeX, float rotationY, float rotationX, float pixelSizeY, float x, float y, string fileName)
        {
            PixelSizeX = pixelSizeX;
            RotationY = rotationY;
            RotationX = rotationX;
            PixelSizeY = pixelSizeY;
            X = x;
            Y = y;
            FileName = fileName;
        }

        public static WorldFile FromFile(string path)
        {
            var allLines = System.IO.File.ReadAllLines(path);

            var pixelSizeX = float.Parse(allLines[0]);
            var rotationY = float.Parse(allLines[1]);
            var rotationX = float.Parse(allLines[2]);
            var pixelSizeY = float.Parse(allLines[3]);
            var x = float.Parse(allLines[4]);
            var y = float.Parse(allLines[5]);

            return new WorldFile(pixelSizeX, rotationY, rotationX, pixelSizeY, x, y, Path.GetFileName(path));
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
