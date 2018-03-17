using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Diagnostics;
using C5;
using MathNet.Numerics.RandomSources;
using MathNet.Numerics.Distributions;
namespace SaxIndex
{

    public static class Util
    {
        #region STATIC VARIABLES
        public delegate double[] Normalize(double[] timeSeries);
        public static Normalize NormalizationHandler = new Normalize(Z_Normalization);
        private static RandomSource generator = null;
        public const char Delimiter = '$';
        public const double Epsilon = 0.00001;

        #endregion

        #region STATIC CONSTRUCTOR

        static Util()
        {
            generator = new SystemRandomSource();
        }

        #endregion

        #region STATIC METHODS
        public static void GenerateDataToFile(string file, int seed, int length, int numTs)
        {
            Util.SeedGenerator(seed);
            int count = 0;
            using (StreamWriter sw = new StreamWriter(file))
            {
                while (count < numTs)
                {
                    sw.WriteLine(Util.ArrayToString(Util.RandomWalk(length)));
                    count++;
                    if (count % 100000 == 0)
                    {
                        Console.WriteLine(count);
                    }
                }
            }
            Console.WriteLine(count);
        }

        public static bool AllZero(double[] data)
        {
            foreach (double d in data)
                if (Math.Abs(d) > Util.Epsilon)
                    return false;
            return true;
        }

        public static double Mean(double[] data, int index1, int index2)
        {
            //try
            //{
            if (index1 < 0 || index2 < 0 || index1 >= data.Length ||
                index2 >= data.Length)
            {
                throw new Exception("Invalid index!");
            }
            //}

            if (index1 > index2)
            {
                int temp = index2;
                index2 = index1;
                index1 = temp;
            }

            double sum = 0;

            for (int i = index1; i <= index2; i++)
            {
                sum += data[i];
            }

            return sum / (index2 - index1 + 1);
        }

        private static double[] Z_Normalization(double[] timeSeries)
        {
            double mean = Mean(timeSeries, 0, timeSeries.Length - 1);
            double std = StdDev(timeSeries);

            double[] normalized = new double[timeSeries.Length];

            if (std == 0)
                std = 1;

            for (int i = 0; i < timeSeries.Length; i++)
            {
                normalized[i] = (timeSeries[i] - mean) / std;
            }

            return normalized;
        }

        public static double[] MeanZero_Normalization(double[] timeSeries)
        {
            double mean = Mean(timeSeries, 0, timeSeries.Length - 1);
            double[] normalized = new double[timeSeries.Length];

            for (int i = 0; i < timeSeries.Length; i++)
            {
                normalized[i] = (timeSeries[i] - mean);
            }
            return normalized;
        }

        public static double StdDev(double[] timeSeries)
        {
            double mean = Mean(timeSeries, 0, timeSeries.Length - 1);
            double var = 0.0;

            for (int i = 0; i < timeSeries.Length; i++)
            {
                var += (timeSeries[i] - mean) * (timeSeries[i] - mean);
            }
            var /= (timeSeries.Length - 1);

            return Math.Sqrt(var);
        }

        public static string ArrayToString(double[] ts)
        {
            StringBuilder temp = new StringBuilder();
            foreach (double value in ts)
            {
                temp.Append(value.ToString("R"));//roundtrip
                temp.Append(" ");
            }
            return temp.ToString().Trim();
        }

        public static List<double[]> ReadFiletoDoubleList(string filename, bool norm)
        {
            return ReadFiletoDoubleList(filename, norm, ' ');
        }

        public static List<double[]> ReadFiletoDoubleList(string filename, bool norm, char delim)
        {
            List<double[]> t = new List<double[]>();
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    double[] d = Util.StringToArray(line, delim);
                    if (norm == true)
                        d = Util.NormalizationHandler(d);
                    t.Add(d);
                }
            }
            return t;
        }


        public static double WeightedEuclideanDistance(double[] p, double[] q, double[] w)
        {
            double dist = 0;
            if (p.Length != q.Length || p.Length != w.Length)
                throw new ApplicationException("Expected timeseries or weight of equal length");

            for (int i = 0; i < p.Length; i++)
                dist += w[i] * Math.Pow((p[i] - q[i]), 2);

            return Math.Sqrt(dist);
        }

        public static double EuclideanDistance(double[] p, double[] q)
        {
            double dist = 0;
            if (p.Length != q.Length)
                throw new ApplicationException("Expected timeseries of equal length");

            for (int i = 0; i < p.Length; i++)
                dist += (p[i] - q[i]) * (p[i] - q[i]);

            return Math.Sqrt(dist);
        }

        public static double[] RandomWalk(int length)
        {
            NormalDistribution n = new NormalDistribution(generator);
            n.SetDistributionParameters(0, 1); // mean 0 std 1 variance 1

            double[] ts = new double[length];
            double[] e = new double[length - 1];

            for (int i = 0; i < e.Length; i++)
                e[i] = n.NextDouble();

            ts[0] = 0;
            for (int i = 1; i < length; ++i)
                ts[i] = ts[i - 1] + e[i - 1];

            return NormalizationHandler(ts); // Z
        }

        public static void SeedGenerator(int seed)
        {
            Console.WriteLine("Random generator seeded: {0}.", seed);
            generator = new SystemRandomSource(seed);
        }

        public static double[] StringToArray(string line)
        {
            return StringToArray(line, ' ');
        }

        public static double[] StringToArray(string line, char delim)
        {
            string[] fields = line.Trim().Split(new char[] { delim },
                StringSplitOptions.RemoveEmptyEntries);
            double[] data = new double[fields.Length];

            for (int j = 0; j < fields.Length; j++)
                data[j] = double.Parse(fields[j], System.Globalization.NumberStyles.Float);

            return data;
        }

        public static double[] ByteArrayToDoubleArray(byte[] b)
        {
            if (b.Length % sizeof(double) != 0)
                throw new ApplicationException("byteArray.Length % SIZEOF_DOUBLE != 0");
            double[] d = new double[b.Length / sizeof(double)];
            Buffer.BlockCopy(b, 0, d, 0, b.Length);
            return d;
        }

        public static byte[] DoubleArrayToByteArray(double[] d)
        {
            byte[] b = new byte[d.Length * sizeof(double)];
            Buffer.BlockCopy(d, 0, b, 0, b.Length);
            return b;
        }

        public static ushort[] UnsignedShortArray(int len, ushort num)
        {
            ushort[] array = new ushort[len];
            for (int i = 0; i < array.Length; i++)
                array[i] = num;

            return array;
        }

        public static double[] DownSample(double[] orig, int numSamples)
        {
            if (Math.IEEERemainder(orig.Length, numSamples) != 0)
                throw new ApplicationException("Math.IEEERemainder(orig.Length,numSamples) != 0");

            double[] tmp = new double[(int)orig.Length / numSamples];
            double sum = 0;
            int pos = 0;
            for (int i = 0; i < orig.Length; ++i)
            {
                sum += orig[i];
                if ((i + 1) % numSamples == 0)
                {
                    tmp[pos++] = sum / (double)numSamples;
                    sum = 0;
                }
            }

            return tmp;
        }

        #endregion
    }
}

