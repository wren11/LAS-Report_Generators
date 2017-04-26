using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;

using Atlass.Core;
using DatabaseModel;

namespace GISManager
{
    [Serializable]
    public class TcReportObject
    {
        public string heightfile { get; set; }

        public string[] files { get; set; }

        public String Project { get; set; }
        public String Area { get; set; }
        
        public List<TcReportPoint3D> Accepted { get; set; }
        public List<TcReportPoint3D> NotCovered { get; set; }
        public List<TcReportPoint3D> NotFlat { get; set; }
        public List<TcReportPoint3D> OutOfSigma { get; set; }
        public List<TcHistogram> Histograms { get; set; }

        public Single XAdjustment { get; set; }
        public Single YAdjustment { get; set; }
        public Single ZAdjustment { get; set; }
        public Single Sigma { get; set; }
        public Single HistogramScaling { get; set; }
        public Int64 NoOfCoveragePoints { get; set; }
        public TeReportStatus Status { get; set; }

        [XmlIgnore]
        public Int32 RejectedCount { get { return NotCovered.Count + NotFlat.Count + OutOfSigma.Count; } }

        [XmlIgnore]
        public Int32 TotalCount { get { return Accepted.Count + RejectedCount; } }

        [XmlIgnore]
        public Single Average { get { return Accepted.Count > 0 ? (Single)Math.Round(Accepted.Average(iter => iter.Difference), 2) : 0.0f; } set { } }

        [XmlIgnore]
        public Single MinDifference { get { return Accepted.Count > 0 ? (Single)Math.Round(Accepted.Min(iter => iter.Difference), 2) : Single.MinValue; } }

        [XmlIgnore]
        public Single MaxDifference { get { return Accepted.Count > 0 ? (Single)Math.Round(Accepted.Max(iter => iter.Difference), 2) : Single.MaxValue; } }

        public TcReportObject()
        {
            Project = String.Empty;
            Area = String.Empty;
            XAdjustment = 0.0f;
            YAdjustment = 0.0f;
            ZAdjustment = 0.0f;
            Sigma = 0.0f;
            NoOfCoveragePoints = 0;
            HistogramScaling = 1.0f;
            Status = TeReportStatus.Unknown;

            Accepted = new List<TcReportPoint3D>();
            NotCovered = new List<TcReportPoint3D>();
            NotFlat = new List<TcReportPoint3D>();
            OutOfSigma = new List<TcReportPoint3D>();
            Histograms = new List<TcHistogram>();
        }

        public TcReportPoint3D GetPoint(Double prmX, Double prmY)
        {
            Double rx = Math.Round(prmX, 2);
            Double ry = Math.Round(prmY, 2);

            if (Accepted.Count > 0)
            {
                TcReportPoint3D foundPoint = Accepted.FirstOrDefault(iter => iter.X == rx && iter.Y == ry);
                if (foundPoint != null)
                {
                    return foundPoint;
                }
            }

            if (NotCovered.Count > 0)
            {
                TcReportPoint3D foundPoint = NotCovered.FirstOrDefault(iter => iter.X == rx && iter.Y == ry);
                if (foundPoint != null)
                {
                    return foundPoint;
                }
            }

            if (NotFlat.Count > 0)
            {
                TcReportPoint3D foundPoint = NotFlat.FirstOrDefault(iter => iter.X == rx && iter.Y == ry);
                if (foundPoint != null)
                {
                    return foundPoint;
                }
            }

            if (OutOfSigma.Count > 0)
            {
                TcReportPoint3D foundPoint = OutOfSigma.FirstOrDefault(iter => iter.X == rx && iter.Y == ry);
                if (foundPoint != null)
                {
                    return foundPoint;
                }
            }

            return null;
        }
    }
}
