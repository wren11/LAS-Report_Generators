using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;

using DevExpress.XtraCharts;
using DevExpress.XtraReports.UI;
using Atlass.Utils;
using DatabaseModel;
using System.Collections.Generic;
using System.Linq;
using Atlass.LAS.Lib.Utilities;
using System.Security.Cryptography;

namespace GISManager
{
    public partial class TcRepStatistics : XtraReport
    {
        public TcReportObject m_ReportData;

        public TcRepStatistics()
        {
            InitializeComponent();
            m_ReportData = null;

        }
        //-----------------------------------------------------------------------------

        public TcRepStatistics(TcReportObject prmReportData) : this()
        {
            m_ReportData = prmReportData;
            UpdateData();
        }
        //-----------------------------------------------------------------------------

        public void UpdateData()
        {
            // Q&A decisions about the project.
            UpdateDecisions();

            // Histogram of the differences.
            UpdateHistogramData();

            cellProjectName.Text = m_ReportData.Project;
            cellAreaName.Text = m_ReportData.Area;
            cellDescription.Text = String.Format(cellDescription.Text, m_ReportData.Project, m_ReportData.Area);
            cellYear.Text = String.Format("{0}/{1}", DateTime.Now.ToString("MM"), DateTime.Now.ToString("yy"));
            cellDayMonth.Text = DateTime.Now.ToString("dd-MMM-yy hh:mm:ss");

            
            // Accepted points.
            repAccepted.DataSource = m_ReportData.Accepted;
            cellAccIndex.DataBindings.Add("Text", repAccepted.DataSource, "Id");
            cellAccEast.DataBindings.Add("Text", repAccepted.DataSource, "X", "{0:0.00}");
            cellAccNorth.DataBindings.Add("Text", repAccepted.DataSource, "Y", "{0:0.00}");
            cellAccControl.DataBindings.Add("Text", repAccepted.DataSource, "Z", "{0:0.00}");
            cellAccLidar.DataBindings.Add("Text", repAccepted.DataSource, "LidarHeight", "{0:0.00}");
            cellAccDiff.DataBindings.Add("Text", repAccepted.DataSource, "Difference", "{0:0.00} m");
            repAccepted.Visible = m_ReportData.Accepted.Count > 0;

            // Not-flat points.
            repNotFlat.DataSource = m_ReportData.NotFlat;
            cellNotFlatIndex.DataBindings.Add("Text", repNotFlat.DataSource, "Id");
            cellNotFlatEast.DataBindings.Add("Text", repNotFlat.DataSource, "X", "{0:0.00}");
            cellNotFlatNorth.DataBindings.Add("Text", repNotFlat.DataSource, "Y", "{0:0.00}");
            cellNotFlatControl.DataBindings.Add("Text", repNotFlat.DataSource, "Z", "{0:0.00}");
            cellNotFlatLidar.Text = "N/A";
            cellNotFlatDiff.Text = "N/A";
            repNotFlat.Visible = m_ReportData.NotFlat.Count > 0;

            // Out-of-Sigma points.
            repOutOfSigma.DataSource = m_ReportData.OutOfSigma;
            cellOutOfSigmaIndex.DataBindings.Add("Text", repOutOfSigma.DataSource, "Id");
            cellOutOfSigmaEast.DataBindings.Add("Text", repOutOfSigma.DataSource, "X", "{0:0.00}");
            cellOutOfSigmaNorth.DataBindings.Add("Text", repOutOfSigma.DataSource, "Y", "{0:0.00}");
            cellOutOfSigmaControl.DataBindings.Add("Text", repOutOfSigma.DataSource, "Z", "{0:0.00}");
            cellOutOfSigmaLidar.DataBindings.Add("Text", repOutOfSigma.DataSource, "LidarHeight", "{0:0.00}");
            cellOutOfSigmaDiff.DataBindings.Add("Text", repOutOfSigma.DataSource, "Difference", "{0:0.00} m");
            repOutOfSigma.Visible = m_ReportData.OutOfSigma.Count > 0;

            // Not-covered points.
            repNotCovered.DataSource = m_ReportData.NotCovered;
            cellNotCoveredIndex.DataBindings.Add("Text", repNotCovered.DataSource, "Id");
            cellNotCoveredEast.DataBindings.Add("Text", repNotCovered.DataSource, "X", "{0:0.00}");
            cellNotCoveredNorth.DataBindings.Add("Text", repNotCovered.DataSource, "Y", "{0:0.00}");
            cellNotCoveredControl.DataBindings.Add("Text", repNotCovered.DataSource, "Z", "{0:0.00}");
            cellNotCoveredLidar.Text = "N/A";
            cellNotCoveredDiff.Text = "N/A";
            repNotCovered.Visible = m_ReportData.NotCovered.Count > 0;

            // Pie-chart.
            chartPiePoints.Series[0].Points.Add(new SeriesPoint("Accepted", m_ReportData.Accepted.Count));
            chartPiePoints.Series[0].Points.Add(new SeriesPoint("Rejected-Not Flat", m_ReportData.NotFlat.Count));
            chartPiePoints.Series[0].Points.Add(new SeriesPoint("Rejected-Not Covered", m_ReportData.NotCovered.Count));
            chartPiePoints.Series[0].Points.Add(new SeriesPoint("Rejected-Out of 3σ", m_ReportData.OutOfSigma.Count));

            // Adjustment table.
            repAdjustment.DataSource = dtblAdjustments;
            dtblAdjustments.Rows.Add("East ", String.Format("{0,6:0.00} m", m_ReportData.XAdjustment));
            dtblAdjustments.Rows.Add("North ", String.Format("{0,6:0.00} m", m_ReportData.YAdjustment));
            dtblAdjustments.Rows.Add("Height ", String.Format("{0,6:0.00} m", m_ReportData.ZAdjustment));
            cellAdjustmentDesc.DataBindings.Add("Text", dtblAdjustments, "Description");
            cellAdjustmentValue.DataBindings.Add("Text", dtblAdjustments, "Value");

            // Summary table.
            repSummary.DataSource = dtblSummary;
            dtblSummary.Rows.Add("GCP Points Sampled ", String.Format("{0,6}", m_ReportData.TotalCount));
            dtblSummary.Rows.Add("GCP Points Accepted ", String.Format("{0,6} ({1:0.00}%)", m_ReportData.Accepted.Count, ((m_ReportData.Accepted.Count * 100.00) / m_ReportData.TotalCount)));
            dtblSummary.Rows.Add("GCP Points Rejected ", String.Format("{0,6} ({1:0.00}%)", m_ReportData.RejectedCount, ((m_ReportData.RejectedCount * 100.00) / m_ReportData.TotalCount)));

            dtblSummary.Rows.Add("Mean Average ", String.Format("{0,6:0.00} m", m_ReportData.Average));
            dtblSummary.Rows.Add("RMSE (1σ) ", String.Format("{0,6:0.00} m", m_ReportData.Sigma));
            dtblSummary.Rows.Add("CI95 ", String.Format("{0,6:0.00} m", 1.96 * m_ReportData.Sigma));
            dtblSummary.Rows.Add("Min Difference", m_ReportData.MinDifference == Single.MinValue ? "Infinity" : String.Format("{0,6:0.00} m", m_ReportData.MinDifference));
            dtblSummary.Rows.Add("Max Difference", m_ReportData.MaxDifference == Single.MaxValue ? "Infinity" : String.Format("{0,6:0.00} m", m_ReportData.MaxDifference));
            cellSummaryDesc.DataBindings.Add("Text", dtblSummary, "Description");
            cellSummaryValue.DataBindings.Add("Text", dtblSummary, "Value");

            xrLabel3.Text = string.Format("     File :  {0}", Path.GetFileNameWithoutExtension(m_ReportData.heightfile));
            xrLabel7.Text = string.Format("    Date :  {0}", new FileInfo(m_ReportData.heightfile).CreationTime.ToLocalTime());
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(m_ReportData.heightfile))
                {
                    xrLabel6.Text = string.Format("    MD5 :  {0}", BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", " ").ToUpper());
                }
            }


        }

        //-----------------------------------------------------------------------------

        private void UpdateDecisions()
        {
            if (m_ReportData != null)
            {
                /* ------------------------------------------------------------------------------------
                QUALITIES
                --------- 
                1. If Sigma <= 0.05, quality = "EXCELLENT"
                2. If Sigma > 0.05 && Sigma <= 0.10, quality = "GOOD"
                3. If Sigma > 0.10 && Sigma <= 0.15, quality = "ACCEPTABLE"
                4. If Sigma > 0.15, quality = "NOT ACCEPTABLE"
                --------------------------------------------------------------------------------------*/
                
                // Calculate the square KM area (5x5)/(1000x1000).
                Double sqKm = m_ReportData.NoOfCoveragePoints * 0.000025;
                Color backColor = Color.Empty;

                if (m_ReportData.Sigma <= 0.05f)
                {
                    m_ReportData.Status = TeReportStatus.Excellent;
                    backColor = Color.Green;
                    ForeColor = Color.Black;
                }
                else if (m_ReportData.Sigma > 0.05f && m_ReportData.Sigma <= 0.10f)
                {
                    m_ReportData.Status = TeReportStatus.Good;
                    backColor = Color.LightGreen;
                    ForeColor = Color.Black;
                }
                else if (m_ReportData.Sigma > 0.10f && m_ReportData.Sigma <= 0.15f)
                {
                    m_ReportData.Status = TeReportStatus.Acceptable;
                    backColor = Color.Orange;
                    ForeColor = Color.Black;
                }
                else
                {
                    m_ReportData.Status = TeReportStatus.NotAcceptable;
                    backColor = Color.Red;
                    ForeColor = Color.Black;
                }

                /* ------------------------------------------------------------------------------------
                ACTIONS
                -------
                1. If the total number of points < (6 + 4 points per sqKm), action = "NEW CONTROL REQUIRED / NOT ENOUGH POINT"
                2. If the rejected ratio (only collected points) >= 30%, action = "NEW CONTROL REQUIRED / TOO MANY 'NOT ACCEPTED' POINTS"
                3. If Sigma > 0.10 and Sigma <= 0.15, action = "NEW CONTROL REQUIRED / ( Sigma Between 0.10m and  0.15m )"
                4. If Sigma > 0.15, action = "DATA OUT SIDE ACCEPTABLE TOLERANCE. DO NOT PROCEED"
                --------------------------------------------------------------------------------------*/

                String action = String.Empty;
                if (m_ReportData.TotalCount < (6 + sqKm / 4))
                {
                    action = "New GCP required - Lack of Coverage";
                }
                else if (((m_ReportData.RejectedCount - m_ReportData.NotCovered.Count) * 1.0 / (m_ReportData.TotalCount - m_ReportData.NotCovered.Count)) >= 0.3)
                {
                    action = "New GCP required - Too many rejections";
                }
                else if (m_ReportData.Sigma > 0.10f && m_ReportData.Sigma <= 0.15f)
                {
                    action = "New GCP - σ is too high";
                }
                else if (m_ReportData.Sigma > 0.15f)
                {
                    action = "New GCP required - outside acceptable range";
                }
                else
                {
                    action = "No action required - GCP Acceptable";
                }

                tblDecisions.BackColor = Color.FromArgb(64, backColor);
                if (m_ReportData.Status == TeReportStatus.Excellent)
                    cellQualityText.Text = TcEnums.Description(m_ReportData.Status);
                if (m_ReportData.Status == TeReportStatus.Good)
                    cellQualityText.Text = TcEnums.Description(m_ReportData.Status);
                if (m_ReportData.Status == TeReportStatus.Acceptable)
                    cellQualityText.Text = TcEnums.Description(m_ReportData.Status);
                if (m_ReportData.Status == TeReportStatus.NotAcceptable)
                    cellQualityText.Text = TcEnums.Description(m_ReportData.Status);  


                cellActionText.Text = action;
                cellActionText.BackColor = Color.FromArgb(64, backColor);
            }
        }
        //-----------------------------------------------------------------------------

        private void UpdateHistogramData()
        {
            foreach (TcHistogram hist in m_ReportData.Histograms)
            {
                UpdateView(hist);
            }
        }
        //-----------------------------------------------------------------------------

        public void UpdateView(TcHistogram prmHist)
        {
            if (prmHist == null || prmHist.Points.Count == 0 || prmHist.Counts.Count == 0)
            {
                return;
            }

            xrChart1.Series.BeginUpdate();

            foreach (Series series in xrChart1.Series)
            {
                series.Points.Clear();
            }

            Double x;
            for (int i = 0; i < prmHist.Points.Count; i++)
            {
                x = Math.Round(prmHist.Points[i] / m_ReportData.HistogramScaling, 2);
                xrChart1.Series[0].Points.Add(new SeriesPoint(x, prmHist.Counts[i]));
                xrChart1.Series[1].Points.Add(new SeriesPoint(x, prmHist.Counts[i]));
            }

            Text = String.Format("Height Difference Summary ({0})", Path.GetFileNameWithoutExtension(prmHist.File));

            xrChart1.Titles[0].Text = String.Format("Mean = {0:0.00}m RMSE (1σ) = {1:0.00}m", prmHist.Average, prmHist.StDev);

            xrChart1.Series.EndUpdate();

            // Build the view with static information.
            BuildView();
        }
        //-----------------------------------------------------------------------------

        private void BuildView()
        {
            SideBySideBarSeriesView bar = xrChart1.Series[0].View as SideBySideBarSeriesView;

            bar.AxisX.Title.Text = "Height Differences";
            bar.AxisX.Label.Angle = -90;
            bar.AxisX.Title.Visible = true;
            bar.AxisY.Title.Text = "Points";
            bar.AxisY.Title.Visible = true;
        }
        //-----------------------------------------------------------------------------

        private void BottomMargin_BeforePrint(object sender, PrintEventArgs e)
        {
            e.Cancel = PrintingSystem.Document.PageCount == 0;
        }

        private void gdetHistograms_BeforePrint(object sender, PrintEventArgs e)
        {

        }
        //-----------------------------------------------------------------------------

    }
    //-----------------------------------------------------------------------------

}
//-----------------------------------------------------------------------------