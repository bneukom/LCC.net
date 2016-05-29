using System.ComponentModel;
using System.IO;
using LandscapeClassifier.Extensions;

namespace LandscapeClassifier.Model
{
    public class AscFile
    {
        public int Ncols { get; }
        public int Nrows { get; }
        public float Xllcorner { get; }
        public float Yllcorner { get; }
        public int Cellsize { get; }
        public short NoDataValue { get; }

        public short[,] Data { get; }

        private readonly string _fileName;

        public AscFile(int ncols, int nrows, float xllcorner, float yllcorner, int cellsize, short noDataValue, short[,] data, string fileName)
        {
            Ncols = ncols;
            Nrows = nrows;
            Xllcorner = xllcorner;
            Yllcorner = yllcorner;
            Cellsize = cellsize;
            NoDataValue = noDataValue;
            Data = data;
            _fileName = fileName;
        }

        public static AscFile FromFile(string path)
        {
            var allLines = System.IO.File.ReadAllLines(path);

            var ncols = int.Parse(allLines[0].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
            var nrows = int.Parse(allLines[1].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
            var xllcorner = float.Parse(allLines[2].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
            var yllcorner = float.Parse(allLines[3].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
            var cellsize = int.Parse(allLines[4].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
            short noDataValue = short.Parse(allLines[5].ConvertWhitespacesToSingleSpaces().Split(' ')[1]);

            var data = new short[nrows, ncols];

            for (var lineIndex = 6; lineIndex < allLines.Length; ++lineIndex)
            {
                var line = allLines[lineIndex];

                var values = line.Split(' ');

                for (int i = 0; i < ncols; i++)
                {
                    short value = short.Parse(values[i]);
                    data[lineIndex - 6, i] = value;
                }

            }

            return new AscFile(ncols, nrows, xllcorner, yllcorner, cellsize, noDataValue, data, Path.GetFileName(path));
        }

        public override string ToString()
        {
            return _fileName;
        }

    }
}
