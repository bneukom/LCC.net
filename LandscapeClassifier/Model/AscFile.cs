using System.IO;
using System.Threading.Tasks;
using LandscapeClassifier.Extensions;
using static System.IO.Path;

namespace LandscapeClassifier.Model
{
    public class AscFile
    {
        public int Ncols { get; }
        public int Nrows { get; }
        public float Xllcorner { get; }
        public float Yllcorner { get; }
        public float Cellsize { get; }
        public short NoDataValue { get; }

        public short[,] Data { get; }

        public readonly string Path;

        public AscFile(int ncols, int nrows, float xllcorner, float yllcorner, float cellsize, short noDataValue, short[,] data, string path)
        {
            Ncols = ncols;
            Nrows = nrows;
            Xllcorner = xllcorner;
            Yllcorner = yllcorner;
            Cellsize = cellsize;
            NoDataValue = noDataValue;
            Data = data;
            Path = path;
        }

        public static AscFile FromFile(string path)
        {

            var ncols = 0;
            var nrows = 0;
            var xllcorner = 0.0f;
            var yllcorner = 0.0f;
            var cellsize = 0.0f;
            short noDataValue = short.MinValue;

            // initialize 
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("ncols"))
                    ncols = int.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else if (line.StartsWith("nrows"))
                    nrows = int.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else if (line.StartsWith("xllcorner"))
                    xllcorner = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else if (line.StartsWith("yllcorner"))
                    yllcorner = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else if (line.StartsWith("cellsize"))
                    cellsize = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else if (line.StartsWith("noDataValue"))
                    noDataValue = short.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                else
                    break;
            }

            short[,] data = new short[nrows, ncols];


            Parallel.ForEach(File.ReadLines(path), (line, _, lineNumber) =>
            {
                if (lineNumber > 4)
                {
                    var splitted = line.Trim().Split(' ');
                    int colIndex = 0;
                    foreach (string stringValue in splitted)
                    {
                        data[lineNumber - 5, colIndex] = (short) float.Parse(stringValue);
                        colIndex++;
                    }
                }
            });

            /*
            Parallel.ForEach(File.ReadLines(path), (line, _, lineNumber) =>
            {
                if (lineNumber > 4)
                {
                    int state = 0;
                    string value = "";
                    int valueLength = 0;
                    int colIndex = 0;
                    for (int charAt = 0; charAt < line.Length; ++charAt)
                    {
                        if (lineNumber - 4 == 2123 && colIndex == 2908)
                        {
                            Console.WriteLine("foo");
                        }
                        switch (state)
                        {
                            case 0:
                                if (line[charAt] == ' ')
                                {
                                    value = "";
                                    valueLength = 1;
                                    state = 1;
                                }
                                break;
                            case 1:
                                if (line[charAt] == '.')
                                {
                                    short numericalValue = 0;
                                    int digitMultiplier = 1;
                                    for (int i = value.Length - 1; i >= 0; --i)
                                    {
                                        numericalValue += (short)((value[i] - '0') * digitMultiplier);
                                        digitMultiplier *= 10;
                                    }

                                    data[lineNumber - 5, colIndex] = numericalValue;
                                    colIndex++;
                                    state = 0;
                                }
                                else
                                {
                                    value += line[charAt];
                                    valueLength++;
                                }
                                break;
                        }
                    }
                }
            });
            */

            return new AscFile(ncols, nrows, xllcorner, yllcorner, cellsize, noDataValue, data, path);

            /*
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs, 4096 * 4096))
            using (StreamReader sr = new StreamReader(bs))
            {
                var ncols = 0;
                var nrows = 0;
                var xllcorner = 0.0f;
                var yllcorner = 0.0f;
                var cellsize = 0.0f;
                short noDataValue = short.MinValue;
                string line = "";
                short[,] data = null;
                int lineIndex = 0;
                while ((line = sr.ReadLine()) != null)
                {

                    if (line.StartsWith("ncols"))
                        ncols = int.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else if (line.StartsWith("nrows"))
                        nrows = int.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else if (line.StartsWith("xllcorner"))
                        xllcorner = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else if (line.StartsWith("yllcorner"))
                        yllcorner = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else if (line.StartsWith("cellsize"))
                        cellsize = float.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else if (line.StartsWith("noDataValue"))
                        noDataValue = short.Parse(line.ConvertWhitespacesToSingleSpaces().Split(' ')[1]);
                    else
                    {
                        int state = 0;
                        string value = "";
                        int valueLength = 0;
                        int colIndex = 0;
                        for (int charAt = 0; charAt < line.Length; ++charAt)
                        {
                            switch (state)
                            {
                                case 0:
                                    if (line[charAt] == ' ')
                                    { 
                                        value = "";
                                        valueLength = 1;
                                        state = 1;
                                    }
                                    break;
                                case 1:
                                    if (line[charAt] == '.')
                                    {
                                        short numericalValue = 0;
                                        int digitMultiplier = 1;
                                        for (int i = value.Length - 1; i >= 0; --i)
                                        {
                                            numericalValue += (short)((value[i] - '0')*digitMultiplier);
                                            digitMultiplier *= 10;
                                        }

                                        data[lineIndex, colIndex] = numericalValue;
                                        colIndex++;
                                        state = 0;
                                    }
                                    else
                                    {
                                        value += line[charAt];
                                        valueLength++;
                                    }
                                    break;
                            }
                        }

                        lineIndex++;
                    }

                    // initialize data
                    if (nrows != 0 && ncols != 0 && data == null)
                    {
                        data = new short[nrows, ncols];
                    }
                }
                
                return new AscFile(ncols, nrows, xllcorner, yllcorner, cellsize, noDataValue, data, path);

            }
            */

        }

        public override string ToString() => GetFileName(Path);
    }
}
