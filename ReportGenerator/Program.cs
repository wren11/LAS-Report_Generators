using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Atlass.Utils;
using System.Diagnostics;
using Atlass.LAS.Lib.Types.Class;
using Atlass.Core;
using TcLasReader = Atlass.LAS.Lib.Operations.IO.TcLasReader;
using PointKdTree;
using GISManager;
using System.Xml.Serialization;
using CommandLine;
using CommandLine.Text;
using System.Windows.Forms;

namespace ReportGenerator
{
    class Program
    {
        static TcReportPoint3D[] FlatPoints = new TcReportPoint3D[0];
        static List<TcLasPointBase> CoveragePoints = new List<TcLasPointBase>();
        static List<TcPoint3D> HeightPoints = new List<TcPoint3D>();



        static float XAdjustment = 0.0f, YAdjustment = 0.0f;

        #region Options
        public class Options
        {
            [Option('d', Required = true, HelpText = "Input Directory")]
            public string InputDirectory { get; set; }

            [Option('o', Required = true, HelpText = "output Directory")]
            public string OutputDir { get; set; }

            [Option('p', Required = true, HelpText = "Project Name")]
            public string ProjectName { get; set; }

            [Option('a', Required = true, HelpText = "Project Area Name")]
            public string ProjectArea { get; set; }

            [Option('m', Required = false, DefaultValue = "*_flat.las", HelpText = "Input File Mask")]
            public string Mask { get; set; }

            [Option('g', Required = false, DefaultValue = 2.0, HelpText = "KD Search Radius")]
            public double Radius { get; set; }

            [Option('h', Required = true, HelpText = "GCP Height File")]
            public string GCPFile { get; set; }

            [Option('x', Required = false,  DefaultValue = 0.0f, HelpText = "X Adjustment")]
            public float XAdjustment { get; set; }

            [Option('y', Required = false, DefaultValue = 0.0f, HelpText = "Y Adjustment")]
            public float YAdjustment { get; set; }

            [Option("delta", Required = false, DefaultValue = 0.0f, HelpText = "Z Delta")]
            public float ZDelta { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        #endregion

        public static Options options = null;

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        static void Main(string[] args)
        {

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(@" 
   ___  _   _                ______ _____  _____ _   _ 
  / _ \| | | |               | ___ \  __ \|  ___| \ | |
 / /_\ \ |_| | __ _ ___ ___  | |_/ / |  \/| |__ |  \| |
 |  _  | __| |/ _` / __/ __| |    /| | __ |  __|| . ` |
 | | | | |_| | (_| \__ \__ \ | |\ \| |_\ \| |___| |\  |
 \_| |_/\__|_|\__,_|___/___/ \_| \_|\____/\____/\_| \_/
          --------------------
 ");


            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("          Test Version 0.0.1");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");

            options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {

                var u  = options.GetUsage();

                Console.WriteLine("");

                var inputdirectory = options.InputDirectory;
                var HFILE = options.GCPFile;

                XAdjustment = options.XAdjustment;
                YAdjustment = options.YAdjustment;

                if (!Directory.Exists(options.OutputDir))
                    Directory.CreateDirectory(options.OutputDir);

                if (!Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("Error: Invalid Input Directory.");
                    return;
                }





                Console.Write(@" 
 +--------------+------------------------------------------------------------------+
 |  Parameter   |            Value                                                 |
 +--------------+------------------------------------------------------------------+
 | Input Dir    | {0, 30}                                   |
 | Output Dir   | {1, 30}                                   |
 | X Adjustment | {2, 30}                                   |
 | Y Adjustment | {3, 30}                                   |
 | Mask         | {4, 30}                                   |
 | Radius       | {5, 30}                                   |
 | Project Name | {6, 30}                                   |
 | Project Area | {7, 30}                                   |
 | GCP File     | {8, 30}                                   |
 | Delta Z      | {9, 30}                                   |
 +--------------+------------------------------------------------------------------+

 ", Path.GetDirectoryName(options.InputDirectory), Path.GetDirectoryName(options.OutputDir), options.XAdjustment, options.YAdjustment,
 options.Mask, options.Radius, options.ProjectName, options.ProjectArea, Path.GetFileName(options.GCPFile), options.ZDelta);

                Console.WriteLine("");
                Console.WriteLine("");

                Console.WriteLine(" Loading Height GCP. {0}", Path.GetFileName(HFILE));
                ReadHeights(HFILE);
                Console.WriteLine(" Loading Heights {0} GCP Points Loaded - Finished 1 secs", HeightPoints.Count);

                var flatFiles = new string[0];

                try
                {

                    if (options.Mask.Contains(".las"))
                    {
                        flatFiles = Directory.GetFiles(inputdirectory, options.Mask);
                        UpdateFlatPoints(flatFiles);
                    }
                    else if (options.Mask.Contains(".txt"))
                    {
                        flatFiles = Directory.GetFiles(inputdirectory, options.Mask);
                        UpdateFlatTxtPoints(flatFiles);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("error: " + e.StackTrace + "\n" + e.InnerException.Message + "\n" + e.Message);
                }

                Console.WriteLine("");

                Console.WriteLine(" Generating KD Binary Tree. Please Wait.");
                var start = Stopwatch.StartNew();
                Point3dTree tree = new Point3dTree(ref FlatPoints, true);
                start.Stop();
                Console.WriteLine(" KDTree Generated. - Finished {0} secs", Math.Ceiling((double)start.ElapsedMilliseconds / 1000));

                using (var hAdjustor = new TcHeightAdjustor(ref tree, HeightPoints, XAdjustment, YAdjustment))
                {
                    hAdjustor.CalculateReportPoints(options.ZDelta);

                    Single avgDiff = (Single)Math.Round(hAdjustor.AvgDifference, 2);

                    if (avgDiff != 0.0f)
                    {
                        hAdjustor.CalculateReportPoints(avgDiff - options.ZDelta);
                    }

                    UpdateReportDataSet(hAdjustor.ReportPoints, hAdjustor.Sigma, avgDiff, hAdjustor.Samples
                        , XAdjustment, YAdjustment, flatFiles);

                    File.WriteAllText(options.OutputDir + "\\z_adjustment.txt", string.Format("{0}", avgDiff));
                }
            }
            else
            {
                options.GetUsage();
            }
        }

        private static void UpdateReportDataSet(TcReportPoint3D[] prmPoints, Single prmSigma, Single prmZAdjustment, long samples, float XAdjustment, float YAdjustment
            , string[] ffiles)
        {
            // Points that are not accepted.
            var m_StatReport = new TcReportObject()
            {
                Project = options.ProjectName,
                Area = options.ProjectArea,
                XAdjustment = XAdjustment,
                YAdjustment = YAdjustment,
                ZAdjustment = prmZAdjustment,
                Sigma = prmSigma,
                NoOfCoveragePoints = (int)(FlatPoints.Length / 3.5),
                files = ffiles,
                heightfile = options.GCPFile,
            };

            foreach (TcReportPoint3D point in prmPoints)
            {
                switch (point.Status)
                {
                    case TePointStatus.Accepted:
                        m_StatReport.Accepted.Add(point);
                        break;
                    case TePointStatus.NotFlat:
                        m_StatReport.NotFlat.Add(point);
                        break;
                    case TePointStatus.NotCovered:
                        m_StatReport.NotCovered.Add(point);
                        break;
                    case TePointStatus.OutOfSigma:
                        m_StatReport.OutOfSigma.Add(point);
                        break;
                }
            }

            // Add the histograms.
            m_StatReport.HistogramScaling = 25;
            m_StatReport.Histograms.Add(TcHistogram.Get(m_StatReport.Accepted.Select(iter => iter.Difference), m_StatReport.HistogramScaling));

            var m_ReportForm = new TcRepStatistics(m_StatReport);

            if (File.Exists(Path.Combine(options.OutputDir, "report.txt")))
                File.Delete(Path.Combine(options.OutputDir, "report.txt"));

            using (var sr = File.CreateText(Path.Combine(options.OutputDir, "report.txt")))
            {
                sr.WriteLine(string.Format("{0,-20} {1,-20} {2,-20} {3,-20} {4,-20} {5,-20} {6,-20} {7,-20}", "ID", "X", "Y", "Z", "Lidar Z", "DIFF", "STATUS", "ADJUSTMENT"));
                sr.WriteLine(string.Format("{0,-20} {1,-20} {2,-20} {3,-20} {4,-20} {5,-20} {6,-20} {7,-20}", "------", "----------", "----------", "------", "----------", "------", "----------", "----------"));

                foreach (var pt in prmPoints)
                {

                        sr.WriteLine(string.Format("{0,-20} {1,-20} {2,-20} {3,-20} {4,-20} {5,-20} {6,-20} {7,-20}",
                            pt.Id, string.Format("{0:000000.000}", pt.X), string.Format("{0:000000.000}", pt.Y), string.Format("{0:000.00}", pt.Z), string.Format("{0:000.00}", pt.LidarHeight), string.Format("{0:00.00}", pt.Difference), pt.Status.ToString(), string.Format("{0:00.00}", pt.ZAjustment)));

                    
                }
            }

            


            // Save the report set to file.
            SaveReportToFile(m_ReportForm, m_StatReport);
        }

        private static void SaveReportToFile(TcRepStatistics m_ReportForm, TcReportObject m_StatReport)
        {
            if (m_StatReport == null || m_ReportForm == null)
            {
                return;
            }
            

            var ReportDataFile = String.Format(@"{0}\prep_report.asr", options.OutputDir);
            var ReportPrnxFile = string.Format(@"{0}\prep_report.prnx", options.OutputDir);

            // Serialize and store them in file.
            XmlSerializer serialzier = new XmlSerializer(typeof(TcReportObject));

            using (FileStream stream = new FileStream(ReportDataFile, FileMode.Create))
            {
                serialzier.Serialize(stream, m_StatReport);
                using (StreamWriter wrt = new StreamWriter(stream))
                {
                    try
                    {
                        // Write the data to the form.
                        wrt.Flush();

                        // Save the report as pdf.
                        m_ReportForm.ExportToPdf(String.Format(@"{0}\Statistical_Report_3Sigma_{1}_{2}.pdf", options.OutputDir, options.ProjectName, options.ProjectArea));

                        // Save as prnx.
                        m_ReportForm.CreateDocument();
                        m_ReportForm.PrintingSystem.SaveDocument(ReportPrnxFile);
                    }
                    finally
                    {
                        wrt.Close();
                    }
                }
            }
        }


        private static void ReadHeights(String prmFile)
        {
            if (!File.Exists(prmFile))
            {
                throw new FileNotFoundException(String.Format("File not found: {0}", prmFile));
            }

            HeightPoints = TcFileReader.ReadPolyPoints(prmFile);
        }

        private static void UpdateFlatPoints(string[] allFiles)
        {
            long flatpointsloaded = 0;
            long totalpointsflat = 0;
            var start = Stopwatch.StartNew();
            var lastprogress = 0;

            foreach (var path in allFiles)
            {
                using (var readerobj = new TcLasReader(path))
                {
                    totalpointsflat += readerobj.TotalPoints;
                }
            }

            Console.WriteLine(" Reading Flat Points {0}", totalpointsflat);
            Console.WriteLine("");

            foreach (var path in allFiles)
            {
                using (var readerobj = new TcLasReader(path))
                {
                    var pts = readerobj.ReadPoints(readerobj.TotalPoints, readerobj.Header);
                    var length = (object)(FlatPoints.Length);

                    Array.Resize(ref FlatPoints, (FlatPoints.Length + pts.Length));

                    for (int i = 0; i < pts.Length; i++)
                    {
                        var pos = ((int)length + i);
                        var p = pts[i];

                        FlatPoints[pos] = new TcReportPoint3D(p.X, p.Y, p.Z);
                    }

                    flatpointsloaded += readerobj.TotalPoints;
                }

                var percent = Clamp(
                    (int)Math.Ceiling((double)flatpointsloaded * 100
                    / totalpointsflat), 0, 100);


                lastprogress = UpdateProgressBar(percent);
            }

            lastprogress = UpdateProgressBar(lastprogress);

            Console.WriteLine("");
            Console.WriteLine("");
            start.Stop();
            Console.WriteLine(" {0} Flat Points Loaded. - Finished {1} secs", flatpointsloaded, Math.Ceiling((double)start.ElapsedMilliseconds / 1000));
        }

        private static void UpdateFlatTxtPoints(string[] allFiles)
        {
            long flatpointsloaded = 0;
            long totalpointsflat = 0;
            var start = Stopwatch.StartNew();
            var lastprogress = 0;

            foreach (var path in allFiles)
            {

                var pts = TcFileReader.ReadPolyPoints(path);
                var length = (object)(FlatPoints.Length);

                Array.Resize(ref FlatPoints, (FlatPoints.Length + pts.Count));

                for (int i = 0; i < pts.Count; i++)
                {
                    var pos = ((int)length + i);
                    var p = pts[i];

                    FlatPoints[pos] = new TcReportPoint3D(p.X, p.Y, p.Z);
                }

                flatpointsloaded += (long)pts.Count;


                var percent = Clamp(
                    (int)Math.Ceiling((double)flatpointsloaded * 100
                    / totalpointsflat), 0, 100);


                lastprogress = UpdateProgressBar(percent);
            }

            lastprogress = UpdateProgressBar(lastprogress);

            Console.WriteLine("");
            Console.WriteLine("");
            start.Stop();
            Console.WriteLine(" {0} Txt Flat Points Loaded. - Finished {1} secs", flatpointsloaded, Math.Ceiling((double)start.ElapsedMilliseconds / 1000));
        }

        private static void UpdateCoveragePoints(string[] allFiles)
        {
            long pointsloaded = 0;
            long totalpoints = 0;
            var start = Stopwatch.StartNew();
            var lastprogress = 0;

            foreach (var path in allFiles)
            {
                using (var readerobj = new TcLasReader(path))
                {
                    totalpoints += readerobj.TotalPoints;
                }
            }

            Console.WriteLine(" Reading Coverage Points {0}", totalpoints);
            Console.WriteLine("");

            foreach (var path in allFiles)
            {
                using (var readerobj = new TcLasReader(path))
                {
                    CoveragePoints.AddRange(readerobj.ReadPoints(readerobj.TotalPoints, readerobj.Header));
                    pointsloaded += readerobj.TotalPoints;
                }

                var percent = Clamp(
                    (int)Math.Ceiling((double)pointsloaded * 100
                    / totalpoints), 0, 100);


                lastprogress = UpdateProgressBar(percent);
            }

            lastprogress = UpdateProgressBar(lastprogress);

            Console.WriteLine("");
            Console.WriteLine("");
            start.Stop();
            Console.WriteLine(" {0} Coverage Points Loaded. - Finished {1} secs", pointsloaded, Math.Ceiling((double)start.ElapsedMilliseconds / 1000));
        }


        private static int UpdateProgressBar(int lastprogress)
        {            
            return lastprogress;
        }
    }
}
