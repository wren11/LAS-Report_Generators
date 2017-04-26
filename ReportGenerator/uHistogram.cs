using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Atlass.Core;

namespace GISManager
{
    [Serializable]
    public class TcHistogram
    {
        public String File { get; set; }
        public Single Average { get; set; }
        public Single StDev { get; set; }
        public List<Single> Points { get; set; }
        public List<Int32> Counts { get; set; }

        public TcHistogram()
            : this (Single.MinValue, Single.MinValue, new List<Single>(), new List<Int32>())
        {
        }
        //-----------------------------------------------------------------------------

        public TcHistogram(Single prmAvg, Single prmStDev, List<Single> prmPoints, List<Int32> prmCounts)
        {
            Average = prmAvg;
            StDev = prmStDev;
            Points = prmPoints;
            Counts = prmCounts;
            File = String.Empty;
        }
        //-----------------------------------------------------------------------------

        static public TcHistogram Get(IEnumerable<Double> prmData, Double prmScaling)
        {
            List<Single> filteredPoints = new List<Single>();
            Single minZ = Single.MaxValue;
            Single maxZ = Single.MinValue;
            Double sum = 0.0;
            Int32 count = 0;

            foreach (Single height in prmData)
            {
                if (height != TcAtlassUtilGlobal.TorNullHeight)
                {
                    count++;
                    sum += height;
                    minZ = Math.Min(minZ, height);
                    maxZ = Math.Max(maxZ, height);
                    filteredPoints.Add(height);
                }
            }

            if (count == 0)
            {
                return null;
            }

            Single avg = (Single)Math.Round(sum / count, 2);
            Single stDev = (Single)Math.Round(TcMathUtil.StDev(filteredPoints, avg), 2);

            Int32 min = (Int32)Math.Round((minZ - avg) * prmScaling);
            Int32 max = (Int32)Math.Round((maxZ - avg) * prmScaling);
            Int32[] steps = Enumerable.Range(min, max - min + 1).ToArray();
            Dictionary<Single, Int32> histogram = new Dictionary<Single, Int32>();

            for (int i = 0; i < steps.Length; i++)
            {
                histogram.Add(steps[i], 0);
            }

            for (int i = 0; i < count; i++)
            {
                var key = (Single)Math.Round((filteredPoints[i] - avg) * prmScaling);

                if (histogram.ContainsKey(key))
                    histogram[key]++;
            }

            return new TcHistogram(avg, stDev, histogram.Keys.ToList(), histogram.Values.ToList());
        }
        //-----------------------------------------------------------------------------

    }
    //-----------------------------------------------------------------------------

}
//-----------------------------------------------------------------------------