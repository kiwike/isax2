using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SaxIndex
{
    public static class Experiments
    {
        public static void DnaExperiment()
        {
            Util.NormalizationHandler = new Util.Normalize(Util.MeanZero_Normalization);
            // in-memory data, referenced by the index
            const string DATAFOLDER = @"K:\Datasets\DNA\Dna2Ts\Monkey_Binary";

            // load index
            Index<Meta2DataFormat> si = Index<Meta2DataFormat>.Load(Globals.IndexRootDir);

            // populate in-memory data
            DnaDataLoader.LoadDnaToMetaBuffer(DATAFOLDER);

            // generate queries
            DateTime queryStart = DateTime.Now;
            int numQueries = 0;
            string[] humanChrs = Directory.GetFiles(@"K:\Datasets\DNA\Dna2Ts\Human_Binary", "*.dat");
            Array.Sort(humanChrs, new NaturalStringComparer());
            Dictionary<string, DnaChrResult> queryResult = new Dictionary<string, DnaChrResult>(humanChrs.Length);
            for (int chrNo = 0; chrNo < humanChrs.Length; ++chrNo)
            {
                string chrFile = humanChrs[chrNo];
                GC.Collect();
                using (BinaryReader br = new BinaryReader(new FileStream(chrFile, FileMode.Open, FileAccess.Read)))
                {
                    List<DnaSearchResult> qResults = new List<DnaSearchResult>();
                    // List<Meta2DataFormat> _queryApproxRes = new List<Meta2DataFormat>();
                    // List<double> _dists = new List<double>();
                    // List<int> _queryPos = new List<int>();

                    long fileLength = br.BaseStream.Length / sizeof(int);
                    int posShift = Globals.TimeSeriesLength / 4; // shift by quarters

                    double[] dnaChr = new double[(int)Math.Floor((fileLength / sizeof(int)) / (double)DnaDataLoader.SAMPLERATE)];
                    Console.WriteLine("F:{0} OrigLen:{1} newLen:{2} Shift:{3}", chrFile, fileLength, dnaChr.Length, posShift);

                    // downsample
                    int count = 0;
                    double sum = 0;
                    for (int i = 0; i < dnaChr.Length; ++i)
                    {
                        sum = 0;
                        count = 0;
                        while (count < DnaDataLoader.SAMPLERATE)
                        {
                            sum += br.ReadInt32();
                            count++;
                        }
                        dnaChr[i] = sum / DnaDataLoader.SAMPLERATE;
                    }

                    double[] ts = new double[Globals.TimeSeriesLength];
                    for (int pos = 0; pos < dnaChr.Length - Globals.TimeSeriesLength; pos += posShift)
                    {
                        numQueries += 2;
                        Array.Copy(dnaChr, pos, ts, 0, Globals.TimeSeriesLength);
                        double mean = Util.Mean(ts, 0, ts.Length - 1);
                        for (int k = 0; k < ts.Length; ++k)
                            ts[k] = ts[k] - mean;

                        TermEntry tEntry = si.ApproximateSearch(ts);
                        List<Meta2DataFormat> termNodeEntries = si.ReturnDataFormatFromTermEntry(tEntry);

                        double bsfDist = Double.MaxValue;
                        Meta2DataFormat bsfMeta = new Meta2DataFormat();
                        foreach (Meta2DataFormat m in termNodeEntries)
                        {
                            double dist = Util.EuclideanDistance(Util.NormalizationHandler(m.GetTimeSeries()), ts);
                            if (dist < bsfDist)
                            {
                                bsfDist = dist;
                                bsfMeta = m;
                            }
                        }

                        qResults.Add(new DnaSearchResult()
                        {
                            dist = bsfDist,
                            matchingChr = bsfMeta._chrNo,
                            matchingPos = bsfMeta._pos,
                            queryChr = chrNo,
                            queryPos = pos,
                        });


                        // reverse
                        ts = ts.Reverse().ToArray();
                        tEntry = si.ApproximateSearch(ts);
                        termNodeEntries = si.ReturnDataFormatFromTermEntry(tEntry);
                        bsfDist = Double.MaxValue;
                        bsfMeta = new Meta2DataFormat();
                        foreach (Meta2DataFormat m in termNodeEntries)
                        {
                            double dist = Util.EuclideanDistance(Util.NormalizationHandler(m.GetTimeSeries()), ts);
                            if (dist < bsfDist)
                            {
                                bsfDist = dist;
                                bsfMeta = m;
                            }
                        }

                        qResults.Add(new DnaSearchResult()
                        {
                            dist = bsfDist,
                            matchingChr = bsfMeta._chrNo,
                            matchingPos = bsfMeta._pos,
                            queryChr = chrNo,
                            queryPos = pos,
                        });


                    }
                    queryResult.Add(chrFile, new DnaChrResult() { results = qResults });
                }
            }
            DateTime queryStop = DateTime.Now;

            Console.WriteLine("{0} Queries, {1} TimeElapsed.", numQueries, queryStop - queryStart);
            //// print results
            using (StreamWriter sw = new StreamWriter(Path.Combine(Globals.IndexRootDir, "queryOutput.txt")))
            {
                foreach (KeyValuePair<string, DnaChrResult> kvp in queryResult)
                {
                    //    Console.WriteLine("HumanChromosome:{0}", kvp.Key);
                    //    Console.WriteLine("AverageDistance:{0}", kvp.Value.AverageDistance);
                    //    Console.WriteLine();
                    foreach (DnaSearchResult sr in kvp.Value.results)
                        sw.WriteLine(sr.ToString());

                }
            }

            //using (StreamWriter sw = new StreamWriter(Path.Combine(Globals.IndexRootDir, "queryOutputTop.txt")))
            //{
            //    foreach (KeyValuePair<string, DnaChrResult> kvp in queryResult)
            //    {
            //        //    Console.WriteLine("HumanChromosome:{0}", kvp.Key);
            //        //    Console.WriteLine("AverageDistance:{0}", kvp.Value.AverageDistance);
            //        //    Console.WriteLine();
            //        List<DnaSearchResult>sr  = kvp.Value.results;
            //        sr.Sort();
            //        sr = sr.GetRange(0, 10);

            //        Console.WriteLine("For Human Chr:{0}", kvp.Key);
            //        var counts = from q in sr
            //                     group q by q.matchingChr into g
            //                     select new { Chr = g.Key, NumHits = g.Count() };
            //        foreach (var v in counts)
            //            Console.WriteLine("{0} : {1}", v.Chr, v.NumHits);

            //    }
            //    //    //{
            //    //    //    for (int i = 0; i < kvp.Value.queryTs.Count; ++i)
            //    //    //    {
            //    //    //        sw.WriteLine(Util.ArrayToString(kvp.Value.queryTs[i]));
            //    //    //        sw.WriteLine(Util.ArrayToString(Util.NormalizationHandler(kvp.Value.queryApproxRes[i].GetTimeSeries())));
            //    //    //    }
            //    //    //    //foreach (double[] d in kvp.Value.queryTs)
            //    //    //    //    sw.WriteLine(Util.ArrayToString(d));
            //}

        }

        public static void TinyImagesExperiment()
        {
            Index<Meta1DataFormat> si = Index<Meta1DataFormat>.Load(Globals.IndexRootDir);

            string queryFile = @"F:\Exp\TinyImages_256Len_8Word_2KThreshold\_queries\queries.txt";
            List<double[]> queries = Util.ReadFiletoDoubleList(queryFile, false);

            for (int i = 0; i < queries.Count; ++i)
            {
                queries[i] = Util.NormalizationHandler(Util.DownSample(queries[i], TinyImagesDataLoader.DOWNSAMPLERATE));
                if (queries[i].Length != Globals.TimeSeriesLength)
                    throw new ApplicationException("queries[i].Length != Globals.TimeSeriesLength");

                TermEntry res = si.ApproximateSearch(queries[i]);
                Console.WriteLine("Query:{0} FileName:{1}", i, res.FileName);

                List<Meta1DataFormat> metas = si.ReturnDataFormatFromTermEntry(res);
                double bsf = Double.MaxValue;
                Meta1DataFormat bsfMeta = new Meta1DataFormat();
                foreach (Meta1DataFormat m in metas)
                {
                    double dist = Util.EuclideanDistance(m.GetTimeSeries(), queries[i]);
                    if (dist < bsf)
                    {
                        bsf = dist;
                        bsfMeta = m;
                    }
                }
                Console.WriteLine("BsfDist:{0} LocMeta:{1}", bsf, bsfMeta.meta);
            }
        }

        public static void InsectExperiment()
        {
            Index<Meta3DataFormat> si = Index<Meta3DataFormat>.Load(Globals.IndexRootDir);
            string queryFile = @"C:\Temp\insect\queries.txt";

            List<double[]> queries = Util.ReadFiletoDoubleList(queryFile, true);
            using (StreamWriter sw = new StreamWriter(@"C:\Temp\insect\output.txt"))
            {
                for (int i = 0; i < queries.Count; ++i)
                {
                    if (queries[i].Length != Globals.TimeSeriesLength)
                        throw new ApplicationException("queries[i].Length != Globals.TimeSeriesLength");

                    TermEntry res = si.ApproximateSearch(queries[i]);
                    Console.WriteLine("Query:{0} FileName:{1}", i, res.FileName);

                    List<Meta3DataFormat> metas = si.ReturnDataFormatFromTermEntry(res);
                    double bsf = Double.MaxValue;
                    Meta3DataFormat bsfMeta = new Meta3DataFormat();
                    foreach (Meta3DataFormat m in metas)
                    {
                        double dist = Util.EuclideanDistance(m.GetTimeSeries(), queries[i]);
                        if (dist < bsf)
                        {
                            bsf = dist;
                            bsfMeta = m;
                        }
                    }
                    Console.WriteLine("BsfDist:{0} Meta1:{1} Meta2:{2}", bsf, bsfMeta.meta1, bsfMeta.meta2);
                    sw.WriteLine(Util.ArrayToString(queries[i]));
                    sw.WriteLine(Util.ArrayToString(bsfMeta.GetTimeSeries()));
                }
            }
        }
    }

   public struct DnaSearchResult : IComparable<DnaSearchResult>
    {
        public int queryPos;
        public int queryChr;
        public int matchingPos;
        public int matchingChr;
        public double dist;

        #region IComparable<DnaSearchResult> Members

        public int CompareTo(DnaSearchResult other)
        {
            return this.dist.CompareTo(other.dist);
        }

        #endregion
        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4}", queryChr, queryPos, matchingChr, matchingPos, dist);
        }
    }

    public struct DnaChrResult
    {
        public List<DnaSearchResult> results;
        //public List<double[]> queryTs;
        //public List<Meta2DataFormat> queryApproxRes;
        //public List<double> dists;
        //public List<int> queryPos;
        //public double AverageDistance { get; set; }

    }

}
