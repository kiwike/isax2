using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using NUnit.Framework;
using System.Linq;
using C5;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace SaxIndex
{
    [Serializable]
    public class Index<DATAFORMAT> where DATAFORMAT : IDataFormat, new()
    {
        #region PUBLIC METHODS

        public TermEntry ApproximateSearch(SaxData dr)
        {
            string saxString = Sax.SaxDataRepToSaxStr(dr, options.SaxOpts);
            if (index.ContainsKey(saxString))
                return SearchHandler(index[saxString], dr);
            else
                return MismatchHandler(dr);
        }

        public TermEntry ApproximateSearch(double[] ts)
        {
            ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
            SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));
            SaxData dr = new SaxData(Sax.ArrayToSaxVals(ts, opts));
            return ApproximateSearch(dr);
        }

        public List<DATAFORMAT> ReturnDataFormatFromTermEntry(TermEntry e)
        {
            if (e.NBuf != -1 || !e.OnDisk)
                throw new ApplicationException("e.NBuf!= -1 || ~e.OnDisk");

            List<DATAFORMAT> tmp = new List<DATAFORMAT>();

            using (BinaryReader br = new BinaryReader(new FileStream(e.FileName, FileMode.Open, FileAccess.Read)))
            {
                long length = br.BaseStream.Length;
                int bytesToRead = SaxData.ByteLength(typeof(DATAFORMAT));
                if (Math.IEEERemainder(length, bytesToRead) != 0)
                    throw new ApplicationException("Math.IEEERemainder(br.BaseStream.Length, bytesToRead) != 0");

                int pos = 0;
                byte[] temp;
                while (pos < length)
                {
                    temp = br.ReadBytes(bytesToRead);
                    if (temp.Length != bytesToRead)
                        throw new ApplicationException("temp.Length != bytesToRead");

                    tmp.Add((DATAFORMAT)SaxData.Parse<DATAFORMAT>(temp).dl);
                    pos += bytesToRead;
                }
            }
            return tmp;
        }
        public CostCounter ExactSearch(double[] ts, out IndexFileDist bsf)
        {
            CostCounter meas = new CostCounter(0, 0);
            IntervalHeap<IndexEntryDist> pq = new IntervalHeap<IndexEntryDist>(NumIndexEntries);

            // approx search
            TermEntry approx = ApproximateSearch(ts);
            bsf = Index<DATAFORMAT>.MinFileEucDist(ts, approx.FileName);
            meas.IO++;
            meas.distance += approx.NumTimeSeries;

            // initalize pq with IndexEntries at root node
            foreach (IndexEntry e in index.Values)
                pq.Add(new IndexEntryDist(e, Sax.MinDistPAAToiSAX(
                    Sax.SaxStrToSaxVals(e.SaxWord), options.SaxOpts, ts)));

            while (!pq.IsEmpty)
            {
                IndexEntryDist minInfo = pq.DeleteMin();
                IndexEntry minEntry = minInfo.entry;

                if (minInfo.dist >= bsf.distance)
                {
                    break;
                }

                if (minEntry is TermEntry)
                {
                    IndexFileDist posMin = Index<DATAFORMAT>.MinFileEucDist(ts, ((TermEntry)minEntry).FileName);
                    meas.IO++;
                    meas.distance += minEntry.NumTimeSeries;

                    // update bsf
                    if (posMin.distance < bsf.distance)
                        bsf = posMin;
                }
                else if (minEntry is SplitEntry<DATAFORMAT>)
                {
                    SplitEntry<DATAFORMAT> sEntry = minEntry as SplitEntry<DATAFORMAT>;
                    foreach (IndexEntry e in sEntry.GetIndexEntries())
                        pq.Add(new IndexEntryDist(e, Sax.MinDistPAAToiSAX(
                            Sax.SaxStrToSaxVals(e.SaxWord), sEntry.Options.SaxOpts, ts)));
                }
            }
            return meas;
        }

        public void FlushEntries()   // Flush TS on Splitnode buffer level on disk
        {
            TermEntry.fileAccessCount = 0;
            this.flush = true;
            int countts = 0;
            foreach (List<SaxData> L in buffer.Values)
            {
                for (int i = L.Count - 1; i >= 0; i--)
                {
                    this.Insert(L[i]);
                    L.RemoveAt(i);
                    countts++;
                    if (countts > 100000) // Force Garbage Collector 
                    {
                        GC.Collect();
                        countts = 0;
                    }
                }
                this.ForceFlushBuffers();

            }
            this.flush = false;
            buffer = new Dictionary<string, List<SaxData>>();
            Console.WriteLine("  " + TermEntry.fileAccessCount);
        }

        public void ForceFlushBuffers()
        {
            TermBuffer.FinishInsertions(); // Flush TermBuffer nodes
        }


        public Dictionary<string, IndexEntry>.ValueCollection GetIndexEntries()
        {
            return index.Values;
        }

        public void Insert(SaxData input)
        {
            string saxString = Sax.SaxDataRepToSaxStr(input, options.SaxOpts);
            if (splitDepth == 0 && flush == false)
            {
                if (!buffer.ContainsKey(saxString))
                {
                    buffer.Add(saxString, new List<SaxData>());
                }
                buffer[saxString].Add(input);
            }
            else
            {
                if (index.ContainsKey(saxString))
                {
                    IndexEntry entry = index[saxString];
                    if (entry is TermEntry)// if terminal, then search path terminates here
                    {
                        TermEntry tentry = (TermEntry)entry;
                        string oldFileName = tentry.FileName;
                        if (SplitEntry(tentry) == false) // check bucket requires a split
                        {
                            tentry.InsertToBuffer(input);
                        }
                        else
                        {
                            List<SaxData> B = tentry.getbuffer();
                            if (B == null) B = new List<SaxData>();
                            DiskCost.increasesavedcost(B.Count);

                            ushort[] newMask = this.options.MaskCopy;
                            ushort[] newSaxString = Sax.SaxStrToSaxVals(saxString);
                            string newName = "";
                            for (int i = 0; i < newMask.Length; i++)
                            {
                                newName = newName + newSaxString[i].ToString() + "." + newMask[i].ToString() + "_";
                            }
                            newName = newName.Substring(0, newName.Length - 1);

                            string[] files = Directory.GetFiles(WorkingFolder,
                            string.Concat(newName, "*.txt"));

                            //string[] files = Directory.GetFiles(tentry.FileName);
                            if (tentry.OnDisk == true)
                                Assert.AreEqual(files.Length, 1);
                            else
                                Assert.AreEqual(files.Length, 0);

                            byte[] temp;
                            int pos = 0;
                            long length = -1;
                            int bytesToRead = SaxData.ByteLength(typeof(DATAFORMAT));
                            foreach (string f in files)
                            {
                                using (BinaryReader br = new BinaryReader(new FileStream(f, FileMode.Open, FileAccess.Read)))
                                {
                                    length = br.BaseStream.Length;
                                    if (length != 0)
                                    {
                                        DiskCost.increaserandomcost();
                                    }
                                    if (Math.IEEERemainder(length, bytesToRead) != 0)
                                        throw new ApplicationException("Math.IEEERemainder(br.BaseStream.Length, bytesToRead) != 0");
                                    while (pos < length)
                                    {
                                        temp = br.ReadBytes(bytesToRead);
                                        if (temp.Length != bytesToRead)
                                            throw new ApplicationException("temp.Length != bytesToRead");

                                        B.Add(SaxData.Parse<DATAFORMAT>(temp));
                                        DiskCost.increasereadcost();
                                        pos += bytesToRead;
                                    }
                                }
                                File.Delete(f);
                            }
                            SplitEntry<DATAFORMAT> newSplit;
                            if (Globals.NewSplitPolicy)
                            {
                                newSplit = new SplitEntry<DATAFORMAT>(saxString, UpdateOptions(B), (byte)(1 + splitDepth));
                            }
                            else
                            {
                                newSplit = new SplitEntry<DATAFORMAT>(saxString, UpdateOptions(null), (byte)(1 + splitDepth));
                            }
                            newSplit.Insert(input);
                            foreach (SaxData S in B)
                            {
                                newSplit.Insert(S);
                            }
                            // update index entry from terminal to split
                            index[saxString] = newSplit;
                        }
                    }
                    else if (entry is SplitEntry<DATAFORMAT>)    // internal node
                    {
                        ((SplitEntry<DATAFORMAT>)entry).Insert(input);
                    }
                }
                else // saxString has not been seen before, create new file and entry
                {
                    ushort[] newMask = this.options.MaskCopy;
                    ushort[] newSaxString = Sax.SaxStrToSaxVals(saxString);
                    string newName = "";
                    for (int i = 0; i < newMask.Length; i++)
                    {
                        newName = newName + newSaxString[i].ToString() + "." + newMask[i].ToString() + "_";
                    }
                    newName = newName.Substring(0, newName.Length - 1);

                    string newfile = Path.Combine(WorkingFolder, string.Concat(newName, ".0.txt"));
                    TermEntry newEntry = new TermEntry(saxString, newfile);
                    newEntry.InsertToBuffer(input);
                    index.Add(saxString, newEntry);
                }
            }
        }


        public List<IndexFileDist[]> KNearestNeighborSequentialScan(int k, List<double[]> tsList)
        {
            CostCounter counter = new CostCounter(0, 0);
            if (k > NumTimeSeries)
            {
                Console.WriteLine("K > number of time series, setting K to number of time series.");
                k = NumTimeSeries;
            }

            List<IntervalHeap<IndexFileDist>> neighbors =
                new List<IntervalHeap<IndexFileDist>>(tsList.Count);
            for (int l = 0; l < tsList.Count; l++)
                neighbors.Add(new IntervalHeap<IndexFileDist>(k + 1));

            Console.Write("Retreiving All Index Files:");
            string[] indexFiles = Directory.GetFiles(Globals.IndexRootDir,
                "*.*.txt", SearchOption.AllDirectories);
            Console.WriteLine(" {0} files.", indexFiles.Length);


            int frac = indexFiles.Length / 10;
            int srchFiles = 0;
            int srchTs = 0;
            int pos = 0;
            int length = 0;
            byte[] temp;
            SaxData tmp;
            double[] data;
            int line;
            double dist;
            BinaryReader r;
            foreach (string f in indexFiles)
            {
                // disp update
                if (srchFiles % (frac == 0 ? 1 : frac) == 0)
                    Console.Write("\r{0}", srchFiles);

                srchFiles++;
                counter.IO++;

                using (FileStream sr = new FileStream(f, FileMode.Open, FileAccess.Read))
                {
                    r = new BinaryReader(sr);
                    pos = 0;
                    length = (int)r.BaseStream.Length; // get the file lenght
                    line = 0;
                    while (pos < length)
                    {
                        srchTs++;
                        temp = r.ReadBytes(SaxData.ByteLength(typeof(DATAFORMAT)));
                        tmp = SaxData.Parse<DATAFORMAT>(temp);
                        data = tmp.dl.GetTimeSeries();

                        for (int query = 0; query < tsList.Count; query++) // compute distance to each query
                        {
                            dist = Util.EuclideanDistance(data, tsList[query]);
                            neighbors[query].Add(new IndexFileDist(f, line + 1, dist));

                            if (neighbors[query].Count > k)  //
                                neighbors[query].DeleteMax();
                        }
                        counter.distance += tsList.Count;

                        line++;
                        pos = pos + SaxData.ByteLength(typeof(DATAFORMAT));

                    }
                    r.Close();
                    sr.Close();
                }

            }

            Console.WriteLine();
            Console.WriteLine("{0} files {1} entries searched.", srchFiles, srchTs);

            List<IndexFileDist[]> result = new List<IndexFileDist[]>(tsList.Count);
            for (int l = 0; l < tsList.Count; l++)
                result.Add(new IndexFileDist[k]);

            for (int t = 0; t < tsList.Count; t++)
                for (int i = 0; i < k; i++)
                    result[t][i] = neighbors[t].DeleteMin();

            return result;
        }

        #endregion // PUBLIC METHODS

        #region PRIVATE METHODS


        private TermEntry MismatchHandler(SaxData dr)
        {
            if (NumIndexEntries > 1)
            {
                string saxString = Sax.SaxDataRepToSaxStr(dr, options.SaxOpts);

                // find last promoted pos
                int pos = 0;
                ReadOnlyCollection<ushort> mask = options.Mask;
                for (int i = 0; i < mask.Count; i++)
                {
                    if (mask[pos] <= mask[i])
                        pos = i;
                }

                // search for match
                foreach (string entrySaxString in index.Keys)
                {
                    if (Sax.SaxStrToSaxVals(entrySaxString)[pos] ==
                        Sax.SaxStrToSaxVals(saxString)[pos])
                    {
                        return SearchHandler(index[entrySaxString], dr);
                    }
                }
            }

            // if no match
            return SearchHandler(ReturnFirstIndexEntry(), dr);
        }

        private IndexEntry ReturnFirstIndexEntry()
        {
            IndexEntry temp = null;
            foreach (IndexEntry entry in index.Values)
            {
                temp = entry;
                break;
            }
            return temp;
        }

        private bool SplitEntry(TermEntry entry)
        {
            int numEntries = ((TermEntry)entry).NumTimeSeries + 1;
            if (numEntries > Globals.IndexNumMaxEntries)
                return true;
            else
                return false;
        }

        private IndexOptions UpdateOptions(List<SaxData> L)
        {
            if (L == null)
            {
                ushort[] newMask = options.MaskCopy;

                // find first pos with lowest  mask value and promote it
                int min = newMask[0];
                int pos = 0;
                for (int i = 1; i < newMask.Length; i++)
                {
                    if (min > newMask[i])
                    {
                        min = newMask[i];
                        pos = i;
                    }
                }
                newMask[pos] = (ushort)(newMask[pos] + 1);
                string newBaseFolder = (splitDepth == 0) ? pos.ToString() :
                    string.Concat(options.BaseDir, pos.ToString());

                return new IndexOptions(newBaseFolder, newMask);
            }
            else
            {
                ushort[] newMask = options.MaskCopy;
                // find first pos with lowest  mask value and promote it
                int min = newMask[0];
                int pos = 0;
                int[] mv = new int[newMask.Length];
                double[] SumOfSqrs = new double[newMask.Length];
                foreach (SaxData S in L)
                {
                    for (int i = 0; i < newMask.Length; i++)
                    {
                        mv[i] += S.values[i];
                        SumOfSqrs[i] += Math.Pow(S.values[i], 2);
                    }

                }
                double[] avg = new double[mv.Length];
                double[] topSum = new double[mv.Length];
                double[] stdev = new double[mv.Length];
                for (int i = 0; i < newMask.Length; i++)
                {
                    avg[i] = mv[i] / System.Convert.ToDouble(L.Count);
                    topSum[i] = (L.Count * SumOfSqrs[i]) - (Math.Pow(mv[i], 2));
                    // stdev computed taking into account of the precision of the cardinality at that segment
                    stdev[i] = (Math.Sqrt(topSum[i] / (System.Convert.ToDouble(L.Count) * (System.Convert.ToDouble(L.Count) - 1)))) * ((newMask[i]+1 )*2);
                }
              //  int minPos = 0;
              //  double minVal = double.MaxValue;
                double maxstdev = 0;
                for (int i = 0; i < newMask.Length; i++)
                {
                    if (stdev[i] > maxstdev && newMask[pos] <= (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2)))
                    {
                        maxstdev = stdev[i];
                        pos = i;
                    }

                    //if (minVal > newMask[i])
                    //{
                    //    minVal = newMask[i];
                    //    minPos = i;
                    //}
                }

                //// hack around overflow
                //if (newMask[pos] == (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2)))
                //{
                //    Assert.AreNotEqual(pos, minPos);
                //    newMask[minPos] = (ushort)(newMask[minPos] + 1);
                //}
                //else
                //{
                   newMask[pos] = (ushort)(newMask[pos] + 1);
                //}
                //string newBaseFolder = (splitDepth == 0) ? pos.ToString() :
                //   string.Concat(options.BaseDir, pos.ToString());
                string newBaseFolder = (splitDepth == 0) ? "0" :
                   string.Concat(options.BaseDir, this.splitDepth % newMask.Length);
                return new IndexOptions(newBaseFolder, newMask);


            }
        }


        #endregion // PRIVATE METHODS

        #region CLASS CONSTRUCTORS

        public Index(byte depth, IndexOptions parameters)
        {
            this.options = parameters;
            this.splitDepth = depth;

            if (splitDepth == 0)
            {
                if (Directory.Exists(Globals.IndexRootDir))
                    throw new ApplicationException("iSax index cannot be constructed in a pre-existing directory.");
                Directory.CreateDirectory(Globals.IndexRootDir);
            }

            if (!Directory.Exists(WorkingFolder))
                Directory.CreateDirectory(WorkingFolder);
        }
        #endregion // CLASS CONSTRUCTORS

        #region PUBLIC PROPERTIES

        public int FilesCreated
        {
            get
            {
                int count = 0;
                foreach (IndexEntry e in index.Values)
                    if (e is TermEntry)
                        if (((TermEntry)e).OnDisk)
                            count++;
                return count;
            }
        }

        public int NumIndexEntries { get { return index.Count; } }

        public int NumLocalTimeSeries
        {
            get
            {
                int count = 0;
                foreach (IndexEntry e in index.Values)
                    if (e is TermEntry)
                        count += e.NumTimeSeries;
                return count;
            }
        }

        public int NumNodes
        {
            get
            {
                int count = 0;
                foreach (IndexEntry entry in index.Values)
                    count += entry.NumNodes;

                return count;
            }
        }

        public int NumTimeSeries
        {
            get
            {
                int count = 0;
                foreach (IndexEntry entry in index.Values)
                    count += entry.NumTimeSeries;

                return count;
            }
        }

        public IndexOptions Options
        {
            get
            {
                return this.options;
            }
        }

        public string WorkingFolder
        {
            get
            {
                return Path.Combine(Globals.IndexRootDir, options.BaseDir);
            }
        }

        #endregion // PUBLIC PROPERTIES

        #region PRIVATE VARIABLES

        private Dictionary<string, IndexEntry> index =
                            new Dictionary<string, IndexEntry>();
        private Dictionary<string, List<SaxData>> buffer =
                            new Dictionary<string, List<SaxData>>();
        private Boolean flush = false;
        private readonly IndexOptions options;
        //private Type locationtype = typeof(RawDataFormat);
        private byte splitDepth;

        #endregion // PRIVATE VARIABLES

        #region STATIC METHODS

        public static IndexFileDist MinFileEucDist(double[] ts, string file)
        {
            ushort[] val;
            double[] dd;
            byte[] temp;
            int pos = 0;
            int length;
            IndexFileDist? best = null;
            int lineNum = 1;
            int wlen = Globals.SaxBaseCard;
            using (FileStream sr = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                BinaryReader r = new BinaryReader(sr);
                pos = 0;
                length = (int)r.BaseStream.Length;
                val = new ushort[wlen];
                dd = new double[ts.Length];
                while (pos < length)
                {
                    temp = r.ReadBytes(SaxData.ByteLength(typeof(DATAFORMAT)));
                    SaxData tmp = SaxData.Parse<DATAFORMAT>(temp);
                    pos = pos + SaxData.ByteLength(typeof(DATAFORMAT));

                    double[] fileTs = Util.NormalizationHandler(tmp.dl.GetTimeSeries());
                    // repo.ReturnData(Util.IndexFlineToDataLocation(line)));
                    double dist = Util.EuclideanDistance(fileTs, ts);
                    IndexFileDist retEntry = new IndexFileDist(file, lineNum, dist);
                    if (best == null)
                    {
                        best = retEntry;
                    }
                    else
                    {
                        if (best.Value.distance > dist)
                            best = retEntry;
                    }
                    lineNum++;
                }
                r.Close();
                sr.Close();

            }
            return best.Value;
        }

        public static Index<DATAFORMAT> Load(string path)
        {
            BinaryFormatter b = new BinaryFormatter();
            Stream f = File.OpenRead(Path.Combine(path, "index.dr"));
            Index<DATAFORMAT> i = (Index<DATAFORMAT>)b.Deserialize(f);
            f.Close();
            return i;
        }

        public static void Save(string path, Index<DATAFORMAT> i)
        {
            BinaryFormatter b = new BinaryFormatter();
            Stream f = new FileStream(Path.Combine(path, "index.dr"), FileMode.Create, FileAccess.Write, FileShare.None);
            b.Serialize(f, i);
            f.Close();
        }

        private static TermEntry SearchHandler(IndexEntry entry, SaxData dr)
        {
            if (entry is TermEntry)
                return (TermEntry)entry;
            else
                return ((SplitEntry<DATAFORMAT>)entry).ApproximateSearch(dr);

        }

        #endregion // STATIC METHODS

    }

}

