using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.Devices;
using NUnit.Framework;
using System.IO;

namespace SaxIndex
{
    static class Program
    {
        static int SEED = 1416; // s
        static int NUM_TIMESERIES = 200000; // i

        static int TIMESERIES_LENGTH = 128;
        static ushort BASE_CARDINALITY = 2; // c
        static ushort MAX_CARDINALITY = 512;
        static int MAX_ENTRIES = 10; // x
        static byte WORDLENGTH = 8; // w
        static string ROOT_DIR = @"C:\Temp\test3";

        static int FLUSHPAR = 200000;
        static Boolean NEWSPLITPOLICY = true;

        private static void InitalizeGlobalSettings()
        {
            ComputerInfo computerInfo = new ComputerInfo();
            Console.WriteLine("ComputerInfo:");
            Console.WriteLine("-------------");
            Console.WriteLine("{0,-30}:\t{1}", "Platform", computerInfo.OSPlatform);
            Console.WriteLine("{0,-30}:\t{1}", "TotalPhysicalMemory", computerInfo.TotalPhysicalMemory);
            Console.WriteLine("{0,-30}:\t{1}", "AvailablePhysicalMemory", computerInfo.AvailablePhysicalMemory);
            Console.WriteLine("{0,-30}:\t{1}", "TotalVirtualMemory", computerInfo.TotalVirtualMemory);
            Console.WriteLine("{0,-30}:\t{1}", "AvailableVirtualMemory", computerInfo.AvailableVirtualMemory);
            Console.WriteLine();
            Console.WriteLine("Current Settings:");
            Console.WriteLine("-----------------");
            Console.WriteLine("{0,21}:\t{1}", "BaseCardinality", BASE_CARDINALITY);
            Console.WriteLine("{0,21}:\t{1}", "TimeSeriesLength", TIMESERIES_LENGTH);
            Console.WriteLine("{0,21}:\t{1}", "IndexEntries", NUM_TIMESERIES);
            Console.WriteLine("{0,21}:\t{1}", "TerminalNodeSize(th)", MAX_ENTRIES);
            Console.WriteLine("{0,21}:\t{1}", "WordLen", WORDLENGTH);
            Console.WriteLine("{0,21}:\t{1}", "Seed", SEED);
            Console.WriteLine("{0,21}:\t{1}", "Flush Par", FLUSHPAR);
            Console.WriteLine("{0,21}:\t{1}", "New Split Policy", NEWSPLITPOLICY);
            Console.WriteLine();
            Console.WriteLine();

            // index settings
            Globals.Initalize(
                TIMESERIES_LENGTH,
                WORDLENGTH,
                MAX_CARDINALITY,
                BASE_CARDINALITY,
                ROOT_DIR,
                MAX_ENTRIES,
                FLUSHPAR,
                NEWSPLITPOLICY);
            TermBuffer.Initialize(MAX_ENTRIES); // must follow global
        }

        /// <summary>
        /// Builds an index with randomly generated time series
        /// </summary>
        public static void BaseIndex()
        {
            DateTime startTime = DateTime.Now;
            // index construction
            Index<RawDataFormat> si = new Index<RawDataFormat>(0, new IndexOptions("root"));
            DataLoader dl = new GeneratedRawDataLoader(si, Globals.TimeSeriesLength, NUM_TIMESERIES, SEED);
            InsertTimeSeries(dl);
            Console.WriteLine();
            Console.WriteLine("Sequential Disk Accesses: " + DiskCost.seqcost);
            Console.WriteLine("Random Disk Accesses: " + DiskCost.rancost);
            Console.WriteLine("Read Disk Accesses: " + DiskCost.readcost);
            Console.WriteLine("Saved cost in buffer: " + DiskCost.savedcost);
            Console.WriteLine();
            Index<RawDataFormat>.Save(Globals.IndexRootDir, si);
            Index<RawDataFormat> si2 = Index<RawDataFormat>.Load(Globals.IndexRootDir);

            DateTime endConstructionTime = DateTime.Now;
            Console.WriteLine("Index Construction Time: {0}", endConstructionTime - startTime);

            // generate some test queries
            const int NUM_QUERIES = 10;
            List<double[]> queries = new List<double[]>(NUM_QUERIES);
            for (int i = 0; i < NUM_QUERIES; i++)
                queries.Add(Util.RandomWalk(Globals.TimeSeriesLength));

            // full sequential scan
            Console.WriteLine("Performing full sequential scan.");
            Console.WriteLine("--------------------------------");
            List<IndexFileDist[]> nnInfo = si.KNearestNeighborSequentialScan(10, queries);
            Console.WriteLine();

            // query results
            Console.WriteLine("Performing exact and approximate search.");
            Console.WriteLine("----------------------------------------");
            int counter = 0;
            for (int i = 0; i < NUM_QUERIES; i++)
            {
                IndexFileDist exactResult;
                si.ExactSearch(queries[i], out exactResult);

                IndexFileDist approxResult = Index<RawDataFormat>.MinFileEucDist(queries[i],
                    si.ApproximateSearch(queries[i]).FileName);

                Assert.IsTrue(exactResult == nnInfo[i][0]);

                if (approxResult == exactResult)
                {
                    counter++;
                    Console.WriteLine(approxResult);
                }
            }
            Console.WriteLine("{0} approximate results == exact results.", counter);
            Console.WriteLine();
        }
        public static void SearchQualityExperiment()
        {
            DateTime startTime = DateTime.Now;

            // index construction
            Index<RawDataFormat> si = new Index<RawDataFormat>(0, new IndexOptions("root"));
            DataLoader dl = new GeneratedRawDataLoader(si, Globals.TimeSeriesLength, NUM_TIMESERIES, SEED);
            InsertTimeSeries(dl);
            Console.WriteLine();
            Console.WriteLine("Sequential Disk Accesses: " + DiskCost.seqcost);
            Console.WriteLine("Random Disk Accesses: " + DiskCost.rancost);
            Console.WriteLine("Read Disk Accesses: " + DiskCost.readcost);
            Console.WriteLine("Saved cost in buffer: " + DiskCost.savedcost);
            Console.WriteLine();
            Index<RawDataFormat>.Save(Globals.IndexRootDir, si);
            Index<RawDataFormat> si2 = Index<RawDataFormat>.Load(Globals.IndexRootDir);

            DateTime endConstructionTime = DateTime.Now;
            Console.WriteLine("Index Construction Time: {0}", endConstructionTime - startTime);

            // avg over queries
            const int NUM_QUERIES = 100;
            List<double[]> queries = new List<double[]>(NUM_QUERIES);
            for (int i = 0; i < NUM_QUERIES; i++)
                queries.Add(Util.RandomWalk(Globals.TimeSeriesLength));


            // measured metrics
            double approxSearchDist = 0;
            double approxSearchNodeDist = 0;
            double approxSearchNodeSize = 0;
            CostCounter exactSearchCosts = new CostCounter();
            for (int i = 0; i < queries.Count; ++i)
            {
                // exact search
                IndexFileDist eRes;
                exactSearchCosts += si.ExactSearch(queries[i], out eRes);

                // approximate search
                TermEntry approxNode = si.ApproximateSearch(queries[i]);

                double mDist = double.MaxValue;
                List<RawDataFormat> nodeEntries = si.ReturnDataFormatFromTermEntry(approxNode);
                double sumDists = 0;
                foreach (RawDataFormat rd in nodeEntries)
                {
                    double dist = Util.EuclideanDistance(queries[i], rd.GetTimeSeries());
                    sumDists += dist;
                    if (dist < mDist)
                        mDist = dist;
                }
                approxSearchDist += mDist;
                approxSearchNodeDist += sumDists / nodeEntries.Count;
                approxSearchNodeSize += nodeEntries.Count;
            }

            approxSearchDist /= queries.Count;
            approxSearchNodeDist /= queries.Count;
            approxSearchNodeSize /= queries.Count;
            using (StreamWriter sw = new StreamWriter(Path.Combine(ROOT_DIR, "searchQuality.txt")))
            {

                string baseFormat = string.Format("{0}:NumTs_{1}:Th_{2}:Wl_{3}:NewPolicy", NUM_TIMESERIES, Globals.IndexNumMaxEntries, Globals.TimeSeriesLength, Globals.NewSplitPolicy);
                sw.WriteLine(baseFormat);
                sw.WriteLine("ExactSearchNumIO {0}", exactSearchCosts.IO / (double)queries.Count);
                sw.WriteLine("ExactSearchNumCalcuations {0}", exactSearchCosts.distance / (double)queries.Count);
                sw.WriteLine("ApproxSearchDistance {0}", approxSearchDist);
                sw.WriteLine("ApproxSearchAverageNodeDistance {0}", approxSearchNodeDist);
                sw.WriteLine("ApproxSearchAverageNodeSize {0}", approxSearchNodeSize);

                sw.WriteLine("ValidationString ");
                foreach (double[] query in queries)
                    sw.Write("{0} ", query[1]);
                sw.WriteLine();
            }
        }

        /// <summary>
        /// Builds an index from the Insect dataset
        /// </summary>
        public static void InsectIndex()
        {
            DateTime startTime = DateTime.Now;
            const string DATA_DIR = @"D:\jin\Insect";

            Index<Meta3DataFormat> si = new Index<Meta3DataFormat>(0, new IndexOptions("root"));
            DataLoader dl = new InsectDataLoader(si, DATA_DIR);
            InsertTimeSeries(dl);
            Index<Meta3DataFormat>.Save(Globals.IndexRootDir, si);
            Index<Meta3DataFormat> si2 = Index<Meta3DataFormat>.Load(Globals.IndexRootDir);

            DateTime endConstructionTime = DateTime.Now;
            Console.WriteLine("Index Construction Time: {0}", endConstructionTime - startTime);
        }

        /// <summary>
        /// Builds an index from the TinyImages dataset
        /// </summary>
        public static void TinyImagesIndex()
        {
            DateTime startTime = DateTime.Now;
            const string DATA_DIR = @"D:\jin\TinyImagesBinary";

            Index<Meta1DataFormat> si = new Index<Meta1DataFormat>(0, new IndexOptions("root"));
            DataLoader dl = new TinyImagesDataLoader(si, DATA_DIR);
            InsertTimeSeries(dl);
            Index<Meta1DataFormat>.Save(Globals.IndexRootDir, si);
            Index<Meta1DataFormat> si2 = Index<Meta1DataFormat>.Load(Globals.IndexRootDir);

            DateTime endConstructionTime = DateTime.Now;
            Console.WriteLine("Index Construction Time: {0}", endConstructionTime - startTime);
        }

        /// <summary>
        /// Builds an index from the DNA dataset
        /// </summary>
        public static void DnaIndex()
        {
            DateTime startTime = DateTime.Now;
            Util.NormalizationHandler = new Util.Normalize(Util.MeanZero_Normalization);
            const string DATA_FILE = @"M:\Datasets\DNA\Dna2Ts\isax2.0experiment\16.mat.dat";

            Index<Meta2DataFormat> si = new Index<Meta2DataFormat>(0, new IndexOptions("root"));
            DataLoader dl = new DnaDataLoader(si, DATA_FILE);
            InsertTimeSeries(dl);
            Index<Meta2DataFormat>.Save(Globals.IndexRootDir, si);
            Index<Meta2DataFormat> si2 = Index<Meta2DataFormat>.Load(Globals.IndexRootDir);

            DateTime endConstructionTime = DateTime.Now;
            Console.WriteLine("Index Construction Time: {0}", endConstructionTime - startTime);
        }

        public static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                ROOT_DIR = args[0];
                NUM_TIMESERIES = int.Parse(args[1]);
                NEWSPLITPOLICY = bool.Parse(args[2]);
            }
            InitalizeGlobalSettings();
            BaseIndex();
            //Console.WriteLine("Press Enter to exit program.");
            //Console.ReadLine();
        }

        public static void InsertTimeSeries(DataLoader dl)
        {
            Console.WriteLine();
            Console.WriteLine("Inserting timeseries to index.");
            Console.WriteLine("------------------------------");
            dl.LoadIndex();
            Console.WriteLine("Complete. {0} inserted.", dl.Processed);
            Console.WriteLine();
        }
    }
}
