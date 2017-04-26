using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Atlass.Core;

namespace ReportGenerator
{
    public class TcHeightAdjustor : IDisposable
    {
        // 20cm tolerance when same point exists in multiple strips.
        const Double c_QATolerance = 0.2;

        private Single m_XAdjustment;
        private Single m_YAjustment;

        private List<TcPoint3D> m_Heights;

        public List<TcPoint3D> HeightPoints
        {
            get { return m_Heights; }
        }

        public TcReportPoint3D[] ReportPoints { get; private set; }
        public Int32 HeightPointCount { get { return m_Heights == null ? 0 : m_Heights.Count; } }

        private Double m_AvgDifference;
        public Double AvgDifference { get { return m_AvgDifference; } }

        private Single m_Sigma;
        public Single Sigma { get { return m_Sigma; } }

        public PointKdTree.Point3dTree Tree;

        public long Samples = 0;


        public TcHeightAdjustor(ref PointKdTree.Point3dTree tree, List<TcPoint3D> HeightPoints, Single prmXAdj, Single prmYAdj)
        {
            m_XAdjustment = prmXAdj;
            m_YAjustment = prmYAdj;
            m_AvgDifference = 0;
            m_Sigma = 0;

            m_Heights = HeightPoints;
            Tree = tree;

        }

        private void FilterBySigma(Int32 prmSigma)
        {
            Int32 pointsRemoved;
            do
            {
                pointsRemoved = 0;
                IEnumerable<TcReportPoint3D> acceptedPoints = ReportPoints.Where(iter => iter.Status == TePointStatus.Accepted);
                if (acceptedPoints.Count() > 0)
                {
                    IEnumerable<Double> differences = acceptedPoints.Select(iter => iter.Difference);
                    m_AvgDifference = Math.Round(differences.Average(), 2);
                    m_Sigma = (Single)Math.Round(TcMathUtil.StDev(differences, m_AvgDifference), 2);
                    Single diff = 0.0f;

                    foreach (TcReportPoint3D point in acceptedPoints)
                    {
                        diff = (Single)Math.Round(Math.Abs(point.Difference - m_AvgDifference), 2);
                        if (diff > prmSigma * m_Sigma)
                        {
                            point.Status = TePointStatus.OutOfSigma;
                            pointsRemoved++;
                        }
                    }
                }
            } while (pointsRemoved != 0);
        }

        public void CalculateReportPoints(Single prmZAdj)
        {
            Samples = 0;
            ReportPoints = new TcReportPoint3D[HeightPointCount];

            var j = 0;

            foreach (var gcp in HeightPoints)
            {
                //no xy adjustments
                var subject = new TcReportPoint3D(gcp.X - m_XAdjustment, gcp.Y - m_YAjustment, gcp.Z);
                var lidarNear = Tree.NearestNeighbours(subject, Program.options.Radius);

                //nearby lidar
                if (lidarNear.Count > 0)
                {
                    subject.LidarHeight = (float)Math.Round((float)lidarNear.Average(i => i.Z), 2);

                    Samples++;

                    //filter out bad quality points.
                    if (subject.Difference > c_QATolerance)
                    {
                        subject.Status = TePointStatus.BadQuality;
                    }
                    else
                    {
                        subject.Status = TePointStatus.Accepted;
                    }
                }
                else
                {
                    subject.Status = TePointStatus.NotCovered;
                }

                ReportPoints[j] = new TcReportPoint3D(m_Heights[j], subject.LidarHeight, prmZAdj, subject.Status) { Id = j };
                j++;
            }

            // Update the average.
            List<Double> acceptedPoints = ReportPoints.Where(iter => iter.Status == TePointStatus.Accepted).Select(iter => iter.Difference).ToList();
            if (acceptedPoints.Count > 0)
            {
                m_AvgDifference = Math.Round(acceptedPoints.Average(), 2);
                m_Sigma = (Single)Math.Round(TcMathUtil.StDev(acceptedPoints, m_AvgDifference), 2);
            }

            FilterBySigma(3);
        }
        //------------------------------------------------------------------

        public void Dispose()
        {
            if (m_Heights != null)
            {
                m_Heights.Clear();
                m_Heights = null;
            }
        }
        //------------------------------------------------------------------


        public int Area { get; set; }
    }
    //------------------------------------------------------------------

}
//------------------------------------------------------------------
