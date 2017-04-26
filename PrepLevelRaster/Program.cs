using Atlass.LAS.Lib.Global;
using Atlass.LAS.Lib.Operations.IO;
using Atlass.LAS.Lib.Types.Class;
using Atlass.LAS.Lib.Utilities;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RConsole = System.Console;

namespace PrepLevelRaster
{
    class Program
    {
        #region Classes

        public class Tile
        {
            public int East, North;
            public List<double> XVertices;
            public List<double> YVertices;
            public List<double> ZVertices;
            public Tile()
            {
                XVertices = new List<double>();
                YVertices = new List<double>();
                ZVertices = new List<double>();
            }
        }
        #endregion

        #region Helpers
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private static List<string> suffixes = new List<string> { " B", " KB", " MB", " GB", " TB", " PB" };
        public static string ToReadableMemUnit(int number)
        {
            for (int i = 0; i < suffixes.Count; i++)
            {
                int temp = number / (int)Math.Pow(1024, i + 1);
                if (temp == 0)
                    return (number / (int)Math.Pow(1024, i)) + suffixes[i];
            }
            return number.ToString();
        }
        #endregion

        #region Options
        public class Options
        {
            [Option('i', Required = true, HelpText = "Input file to be processed.")]
            public string InputFile { get; set; }

            [Option('o', Required = false, HelpText = "output file to be created.")]
            public string OutputFile { get; set; }

            [Option('d', Required = false, HelpText = "output dir to create")]
            public string OutputDir { get; set; }

            [Option('f', Required = false, DefaultValue = 1, HelpText = "Grid Scaling Factor")]
            public int Factor { get; set; }

            [Option('g', Required = false, DefaultValue = 2, HelpText = "Size of Grid")]
            public int GridSize { get; set; }

            [Option('b', Required = false, DefaultValue = 1, HelpText = "Grid[,] Buffer")]
            public int Buffer { get; set; }

            [Option('l', Required = false, DefaultValue = "logFile.txt", HelpText = "Log File Location")]
            public string LogFile { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        #endregion

        public static Options options = null;
        public static int factor;
        public static int TS;
        public static Tile[,] Tiles = null;
        public static long preMem = 0, postMem = 0;
        public static string calMem = string.Empty;

        public static void GeneratePatches(double x, double y, double radius)
        {
            var X = x - radius;
            var Y = y - radius;
            var width = 2 * radius;
            var height = 2 * radius;
        }



        static void Main(string[] args)
        {
            options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                factor = options.Factor;
                TS = options.GridSize;

                var fs = Stopwatch.StartNew();
                string input = options.InputFile;

                if (string.IsNullOrEmpty(options.OutputFile))
                {
                    if (options.OutputDir != string.Empty)
                    {
                        options.OutputFile = new FileInfo(options.InputFile).Directory.FullName;
                        options.OutputFile = Path.Combine(options.OutputDir, Path.GetFileNameWithoutExtension(options.InputFile) + ".xyz");
                        options.LogFile    = Path.Combine(options.OutputDir, Path.GetFileNameWithoutExtension(options.InputFile) + ".txt");
                    }
                }


                if (!new FileInfo(options.OutputFile).Directory.Exists)
                    new FileInfo(options.OutputFile).Directory.Create();



                #region Title ASCII
                Console.ForegroundColor = ConsoleColor.Green;
                RConsole.Write(@"


                     db    88888 8       db    .d88b. .d88b. 
                    dPYb     8   8      dPYb   YPwww. YPwww. 
                   dPwwYb    8   8     dPwwYb      d8     d8 
                  dP    Yb   8   8888 dP    Yb `Y88P' `Y88P' 
                                                             
                      888b. 8888 888b. .d88b. 888b. 88888 
                      8  .8 8www 8  .8 8P  Y8 8  .8   8   
                      8wwK' 8    8wwP' 8b  d8 8wwK'   8   
                      8  Yb 8888 8     `Y88P' 8  Yb   8   
                                                          
                  888b. 8       db    8b  8 8b  8 8888 888b. 
                  8  .8 8      dPYb   8Ybm8 8Ybm8 8www 8  .8 
                  8wwP' 8     dPwwYb  8   8 8   8    8wwK 
                  8     8888 dP    Yb 8   8 8   8 8888 8  Yb 
                                                             
            ");
                Console.ForegroundColor = ConsoleColor.White;
                #endregion

                #region ASCII Parameter Table
                Console.ForegroundColor = ConsoleColor.Magenta;
                RConsole.WriteLine("               Version: Test Build 0.0.1");
                Console.ForegroundColor = ConsoleColor.White;
                RConsole.WriteLine("                    ---------------------------------------                    ");
                RConsole.WriteLine("");
                RConsole.Write(@"
.......................
: Parameter :   Value :
:...........:.........:
: Scale     :    {0:00}   :
: Grid Size :    {1:00}   :
:...........:.........:
", factor, TS);


                RConsole.WriteLine("");

                #endregion

                #region ASCII I/O Table
                RConsole.WriteLine(@"
+--------+---------------------------------------------------------------------+
| Param  |  Values                                                             |
+--------+---------------------------------------------------------------------+
| Input  | {0, -43}                         |
| Output | {1, -43}                         |
| Log    | {2, -43}                         |
+--------+---------------------------------------------------------------------+
", Path.GetFileName(options.InputFile), options.OutputFile, 
Path.GetFileName(options.LogFile));
                #endregion

                if (!File.Exists(input))
                {
                    RConsole.WriteLine("IO Error: input {0} file does not exist.", input);
                    return;
                }

                RConsole.WriteLine("Job Started {0} - {1}", Path.GetFileNameWithoutExtension(input), DateTime.Now.ToString());

                try
                {
                    #region LAS READER / PRE GRID
                    using (var readerobj = new TcLasReader(input))
                    {
                        var s = Stopwatch.StartNew();
                        RConsole.WriteLine("Reading {0} Points", readerobj.TotalPoints);

                        var points = readerobj.ReadPoints(readerobj.TotalPoints, readerobj.Header);
                        s.Stop();
                        RConsole.WriteLine("Reading {0} Points - Finished {1} secs", readerobj.TotalPoints, Math.Ceiling((double)s.ElapsedMilliseconds / 1000));

                        var set = points.GetEnumerator();

                        #region gridding
                        s = Stopwatch.StartNew();

                        var minX = readerobj.Header.MinX;
                        var minY = readerobj.Header.MinY;
                        var maxX = readerobj.Header.MaxX;
                        var maxY = readerobj.Header.MaxY;

                        int X0 = (int)(minX - (minX % (TS * factor)));
                        int Y0 = (int)(minY - (minY % (TS * factor)));
                        int X1 = (int)(maxX - (maxX % (TS * factor)));
                        int Y1 = (int)(maxY - (maxY % (TS * factor)));

                        double dX = X1 - X0;
                        double dY = Y1 - Y0;

                        dX /= TS * factor;
                        dY /= TS * factor;

                        var tiles_x = (int)dX;
                        var tiles_y = (int)dY;

                        preMem = GC.GetTotalMemory(true);
                        Tiles = new Tile[tiles_x, tiles_y];
                        postMem = GC.GetTotalMemory(true);
                        calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));
                        RConsole.WriteLine("Gridding Data Into Tiles {0}/{1} {2}", tiles_x, tiles_y, "- Memory Used: " + calMem);

                        preMem = GC.GetTotalMemory(true);

                        int reportStep = 0;
                        int pointsProcessed = 0;
                        int interval = points.Length > 1000000
                                    ? 1000000 : points.Length > 100000
                                    ? 10000 : points.Length > 10000
                                    ? 10000 : points.Length > 1000
                                    ? 1000 : points.Length > 100
                                    ? 100 : points.Length > 10
                                    ? 10 : 10;

                        while (set.MoveNext())
                        {
                            pointsProcessed++;

                            if (reportStep++ % interval == 0)
                            {
                                var percent = Clamp((int)Math.Ceiling(
                                    (double)pointsProcessed * 100
                                    / points.Length), 0, 100);
                            }



                            var point = (TcLasPointBase)set.Current;

                            if (point.ReturnNumber != point.NumberOfReturns)
                                continue;


                            var x = Math.Round(point.X, 2);
                            var y = Math.Round(point.Y, 2);
                            var z = Math.Round(point.Z, 2);

                            var X = (int)(x - (x % (TS * factor)));
                            var Y = (int)(y - (y % (TS * factor)));
                            var J = (int)(X - minX) / (TS * factor);
                            var I = (int)(Y - minY) / (TS * factor);

                            options.Buffer = Clamp(options.Buffer, 1, 20);

                            J = Clamp(J, 0, tiles_x - options.Buffer);
                            I = Clamp(I, 0, tiles_y - options.Buffer);

                            if (Tiles[J, I] == null)
                            {
                                Tiles[J, I] = new Tile
                                {
                                    East = J,
                                    North = I,
                                };

                                Tiles[J, I].XVertices.Add(x);
                                Tiles[J, I].YVertices.Add(y);
                                Tiles[J, I].ZVertices.Add(z);

                            }
                            else
                            {
                                Tiles[J, I].XVertices.Add(x);
                                Tiles[J, I].YVertices.Add(y);
                                Tiles[J, I].ZVertices.Add(z);
                            }
                        }

                        s.Stop();

                        postMem = GC.GetTotalMemory(true);
                        calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));

                        RConsole.WriteLine("Gridded Data Into {0} Tiles - Finished {1} secs - Memory Used: {2}", true, Tiles.Length, Math.Ceiling((double)s.ElapsedMilliseconds / 1000)
                            , calMem);
                        #endregion
                    }
                    #endregion
                }
                catch (Exception error)
                {
                    RConsole.WriteLine("Error Las Reader / Pre Gridder\n {0}\n{1}\n{2}",
                        error.StackTrace,
                        error.Source,
                        error.Message);


                    return;
                }
                finally
                {
                    fs.Stop();
                    RConsole.WriteLine("Job Completed - {0} secs - {1}", Math.Ceiling((double)fs.ElapsedMilliseconds / 1000), DateTime.Now.ToString());
                }

                try
                {
                    #region Extracting Planars 
                    GC.Collect();
                    GC.SuppressFinalize(Tiles);
                    preMem = GC.GetTotalMemory(true);
                    calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));
                    RConsole.WriteLine("{0} Memory Truncated. ", true, calMem);


                    preMem = GC.GetTotalMemory(true);
                    RConsole.WriteLine("{0}", true, "Detecting Planar Surfaces");


                    var sw = Stopwatch.StartNew();

                    if (Tiles.GetLength(0) == 0
                        || Tiles.GetLength(1) == 0)
                    {
                        return;
                    }

                    var width = Tiles.GetLength(0);
                    var height = Tiles.GetLength(1);
                    var processed = 0;
                    var tinterval = height > 100 ? 100 : 10;
                    var removed = 0;
                    var sigmas = new List<double>();
                    var hvalues = new List<double>();
                    var hinterval = 3;
                    var tilesprocessed = 0;

                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            if (Tiles[i, j] != null)
                            {
                                var tile = Tiles[i, j];
                                var heights = new List<Double>();
                                var maxHeight = Double.MinValue;
                                var minHeight = Double.MaxValue;

                                foreach (var h in tile.ZVertices)
                                {
                                    heights.Add(h);
                                    maxHeight = Math.Max(maxHeight, h);
                                    minHeight = Math.Min(minHeight, h);
                                }

                                //filter out flat planars above .20cm tolerance.
                                if (heights.Count <= 2 || (maxHeight - minHeight > 0.20f))
                                {
                                    Tiles[i, j] = null;
                                    continue;
                                }


                                var avg = TcMathUtil.Average(heights);
                                var stDev = TcMathUtil.StDev(heights, avg);
                                var count = 0;
                                var sum = 0.0;
                                var indices = new List<double>();
   

                                for (int p = 0; p < heights.Count; p++)
                                {
                                    if (Math.Abs(heights[p] - avg) < 2 * stDev)
                                    {
                                        sum += heights[p];
                                        count++;
                                    }
                                    else
                                    {
                                        indices.Add(heights[p]);
                                    }
                                }

                                var result = count > 0
                                    ? sum / count : TcConstants.TorNullValue32Bit;

                                int k = 0;


                                while (k < tile.ZVertices.Count)
                                {
                                    if (!indices.Contains(Tiles[i, j].ZVertices[k])
                                        && result != TcConstants.TorNullValue32Bit)
                                    {
                                        Tiles[i, j].ZVertices[k] = result;
                                    }
                                    else
                                    {
                                        Tiles[i, j].XVertices.RemoveAt(k);
                                        Tiles[i, j].YVertices.RemoveAt(k);
                                        Tiles[i, j].ZVertices.RemoveAt(k);
                                        removed++;
                                    }

                                    k++;
                                }
                            }
                        }

                        if (processed++ % tinterval == 0)
                        {
                            var percent = Clamp(
                                (int)Math.Ceiling((double)processed * 100
                                / height), 0, 100);
                        }


                        if (tilesprocessed++ % hinterval == 0 && sigmas.Count > 0)
                        {
                            hvalues = new List<double>();
                            sigmas = new List<double>();
                        }
                    }


                    preMem = GC.GetTotalMemory(true);
                    calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));

                    RConsole.WriteLine("Non-Flat Points Removed From Data: {0}", removed);
                    RConsole.WriteLine("{0}", true, "Permuting Flat Points - Finished " + Math.Ceiling((double)sw.ElapsedMilliseconds / 1000)
                        + " secs - Memory Used: " + calMem);

                    GC.Collect();
                    GC.SuppressFinalize(Tiles);
                    postMem = GC.GetTotalMemory(true);
                    calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));
                    RConsole.WriteLine("{0} Memory Truncated. ", true, calMem);


                    preMem = GC.GetTotalMemory(true);

                    RConsole.WriteLine("{0}", true, "Saving Planar Surfaces to " + options.OutputFile);
                    sw = Stopwatch.StartNew();


                    using (var sr = File.CreateText(options.OutputFile))
                    {
                        processed = 0;
                        tinterval = height > 100 ? 100 : 10;

                        for (int j = 0; j < height; j++)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                if (Tiles[i, j] != null)
                                {
                                    var tile = Tiles[i, j];

                                    for (int p = 0; p < tile.XVertices.Count; p++)
                                    {
                                        sr.WriteLine("{0:000000.00} {1:000000.00} {2:000.00}",
                                            tile.XVertices[p],
                                            tile.YVertices[p],
                                            tile.ZVertices[p]);

                                    }
                                }
                            }

                            if (processed++ % tinterval == 0)
                            {
                                var percent = Clamp(
                                    (int)Math.Ceiling((double)processed * 100
                                    / height), 0, 100);
                            }
                        }

                    }

                    sw.Stop();

                    preMem = GC.GetTotalMemory(true);
                    calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));


                    RConsole.WriteLine("Saved permuted flat points - Finished {1} secs - Memory Used: {2}", true, Tiles.Length, Math.Ceiling((double)sw.ElapsedMilliseconds / 1000)
                        , calMem);




                    preMem = GC.GetTotalMemory(true);
                    GC.Collect();
                    GC.SuppressFinalize(Tiles);
                    postMem = GC.GetTotalMemory(true);
                    calMem = ToReadableMemUnit((int)Math.Ceiling((double)postMem % preMem));
                    RConsole.WriteLine("{0} Memory Truncated. ", true, calMem);
                    #endregion

                }
                catch (Exception error)
                {
                    RConsole.WriteLine("Error Flattening\n {0}\n{1}\n{2}",
                        error.StackTrace,
                        error.Source,
                        error.Message);


                    return;
                }
                finally
                {
                    fs.Stop();
                    RConsole.WriteLine("Job Completed - {0} secs - {1}", Math.Ceiling((double)fs.ElapsedMilliseconds / 1000), DateTime.Now.ToString());
                }
            }
            else
            {
                options.GetUsage();
            }
        }
    }
}
