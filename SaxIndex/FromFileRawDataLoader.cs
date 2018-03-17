using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SaxIndex
{
    class FromFileRawDataLoader : DataLoader
    {
        #region PUBLIC METHODS

        public override void LoadIndex()
        {
            ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
            SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));
            this.sr = new StreamReader(this.dataFile);

            while (!(this.allRead && this.buffer.Count == 0))
            {
                if (this.buffer.Count == 0)
                {
                    Console.WriteLine(this.processed);
                    string line;
                    while ((line = this.sr.ReadLine()) != null)
                    {
                        double[] ts = Util.NormalizationHandler(Util.StringToArray(line));
                        if (!this.tsLength.HasValue)
                            this.tsLength = (uint)ts.Length;
                        else
                            if (this.tsLength.Value != ts.Length)
                                throw new ApplicationException("Inconsistent length when reading from file.");

                        this.buffer.Enqueue(ts);
                        if (this.buffer.Count == this.bufferSize)
                            break;
                    }
                    if (line == null)
                        this.allRead = true;

                }
                else
                {
                    double[] tmp = this.buffer.Dequeue();
                    IDataFormat dl = new RawDataFormat(tmp);
                    this.si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(tmp, opts)));
                    this.processed++;
                }
            }

            this.sr.Close();
            this.si.ForceFlushBuffers();
            Console.WriteLine("Total: {0}", this.processed);
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public FromFileRawDataLoader(Index<RawDataFormat> si,
            string dataFile, int bufferSize)
        {
            this.si = si;
            this.dataFile = dataFile;
            this.bufferSize = bufferSize;
            this.buffer = new Queue<double[]>(this.bufferSize);
        }

        #endregion

        #region PRIVATE VARIABLES

        private Index<RawDataFormat> si;
        private StreamReader sr = null;
        private int bufferSize;
        private Queue<double[]> buffer;
        private string dataFile;
        private bool allRead = false;
        private uint? tsLength = null;
        #endregion

    }

    class TinyImagesDataLoader : DataLoader
    {
        #region PUBLIC METHODS

        public override void LoadIndex()
        {
            ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
            SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));
            double[] tmp = new double[ORIGINAL_LENGTH];
            double[] ts;
            IDataFormat dl;

            int numFiles = Directory.GetFiles(_dataDir, "*.dat").Length;

            if (numFiles != NUMFILES)
                throw new ApplicationException("numFiles != NUMFILES");

            for (int i = 1; i <= numFiles; ++i)
            {
                string file = Path.Combine(_dataDir, string.Format("i{0}.dat", i));
                if (!File.Exists(file))
                    throw new ApplicationException("!File.Exists(file)");

                using (BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read)))
                {
                    if (br.BaseStream.Length != EXPECTEDBYTES)
                        throw new ApplicationException("br.BaseStream.Length != ORIGINAL_LENGTH * NUMTSPERFILE * sizeof(double)");

                    int bytesRead = br.Read(imageBuffer, 0, EXPECTEDBYTES);
                    if (bytesRead != EXPECTEDBYTES)
                        throw new ApplicationException("EXPECTEDBYTES");

                    int pos = 0;
                    while (pos < EXPECTEDBYTES)
                    {
                        Buffer.BlockCopy(imageBuffer, pos, tsBuffer, 0, ORIGINAL_LENGTH * sizeof(double));
                        ts = Util.NormalizationHandler(Util.DownSample(Util.ByteArrayToDoubleArray(tsBuffer), DOWNSAMPLERATE));

                        dl = new Meta1DataFormat(processed, ts);
                        _si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(ts, opts)));
                        processed++;

                        if (processed % Globals.FlushTsVal == 0)
                            _si.FlushEntries();

                        pos += ORIGINAL_LENGTH * sizeof(double);
                    }
                    Console.WriteLine("{0} read. TsNum:{1}", Path.GetFileName(file), processed);
                }
            }
            _si.FlushEntries();
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public TinyImagesDataLoader(Index<Meta1DataFormat> si,
            string dataDir)
        {
            this._si = si;
            this._dataDir = dataDir;
            if (Globals.TimeSeriesLength != ORIGINAL_LENGTH / DOWNSAMPLERATE)
                throw new ApplicationException("Globals.TimeSeriesLength != ORIGINAL_LENGTH/DOWNSAMPLERATE");
        }

        #endregion

        #region PRIVATE VARIABLES
        private Index<Meta1DataFormat> _si;
        private string _dataDir;

        //  hard coded tiny-images data format
        private const int ORIGINAL_LENGTH = 768;
        private const int NUMFILES = 1407;
        private const int NUMTSPERFILE = 50000;
        private const int EXPECTEDBYTES = ORIGINAL_LENGTH * NUMTSPERFILE * sizeof(double);
        private static byte[] tsBuffer = new byte[ORIGINAL_LENGTH * sizeof(double)];
        private static byte[] imageBuffer = new byte[EXPECTEDBYTES];
        #endregion

        public const int DOWNSAMPLERATE = 3;
    }

    class DnaDataLoader : DataLoader
    {
        #region PUBLIC METHODS

        public static void LoadDnaToMetaBuffer(string dataFolder)
        {
            string[] files = Directory.GetFiles(dataFolder, "*.dat");
            Array.Sort(files, new NaturalStringComparer());
            Meta2DataFormat.dnaBuffer = new List<double[]>(files.Length);

            // read dna data into memory buffer
            for (int fileNo = 0; fileNo < files.Length; ++fileNo)
            {
                Console.WriteLine("FileNo:{0} {1}", fileNo, Path.GetFileNameWithoutExtension(files[fileNo]));
                using (BinaryReader br = new BinaryReader(new FileStream(files[fileNo], FileMode.Open, FileAccess.Read)))
                {
                    long fileLength = br.BaseStream.Length;
                    if (Math.IEEERemainder(fileLength, sizeof(int)) != 0)
                        throw new ApplicationException("Math.IEEERemainder(fileLength,sizeof(int)) != 0");

                    double[] dnaChr = new double[(int)Math.Floor((fileLength / sizeof(int)) / (double)SAMPLERATE)];
                    Console.WriteLine("OriginalLength:{0} DownSampledLength:{1} NumDiscarded:{2}",
                        fileLength / sizeof(int), dnaChr.Length, fileLength / sizeof(int) - dnaChr.Length * SAMPLERATE);

                    // downsample
                    int count = 0;
                    double sum = 0;
                    for (int i = 0; i < dnaChr.Length; ++i)
                    {
                        sum = 0;
                        count = 0;
                        while (count < SAMPLERATE)
                        {
                            sum += br.ReadInt32();
                            count++;
                        }
                        dnaChr[i] = sum / SAMPLERATE;
                    }
                    Meta2DataFormat.dnaBuffer.Add(dnaChr);
                }
            }
        }

        public override void LoadIndex()
        {
            ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
            SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));
            double[] ts;
            IDataFormat dl;

            // load dna into memory
            LoadDnaToMetaBuffer(_dataFolder);

            // iterate through each chr and insert
            double mean = 0;
            int signChange = 0;
            double delta = 0;
            double lastVal = 0;

            for (int chrNo = 0; chrNo < Meta2DataFormat.dnaBuffer.Count; ++chrNo)
            {
                //    Console.WriteLine("ChrNo:{0} Processed:{1} Discarded:{2} IndexDiscarded:{3}", chrNo, processed, discarded, Index<Meta2DataFormat>.discarded);
                //    if (_si.NumTimeSeries != processed - discarded - Index<Meta2DataFormat>.discarded)
                //        throw new ApplicationException();
                for (int pos = 0; pos <= Meta2DataFormat.dnaBuffer[chrNo].Length - Globals.TimeSeriesLength; pos += SHIFT)
                {
                    dl = new Meta2DataFormat(chrNo, pos);
                    ts = dl.GetTimeSeries();

                    // normalize
                    mean = Util.Mean(ts, 0, ts.Length - 1);
                    signChange = 0;
                    lastVal = ts[1] - ts[0];
                    for (int k = 2; k < ts.Length; ++k)
                    {
                        delta = ts[k] - ts[k - 1];
                        if (Math.Sign(lastVal) != Math.Sign(delta))
                            signChange++;
                        lastVal = delta;
                    }

                    for (int k = 0; k < ts.Length; ++k)
                        ts[k] = ts[k] - mean;

                    // filter
                    if (signChange > NUMSIGNCHANGE)
                    {
                        _si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(ts, opts)));
                        processed++;

                        if (processed % Globals.FlushTsVal == 0)
                            _si.FlushEntries();
                    }
                    else
                    {
                        discarded++;
                    }
                }
                GC.Collect();
            }
            // Console.WriteLine("Processed:{0} Discarded:{1} IndexDiscarded:{2}", processed, discarded, Index<Meta2DataFormat>.discarded);
            _si.FlushEntries();
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public DnaDataLoader(Index<Meta2DataFormat> si,
            string dataFolder)
        {
            this._si = si;
            this._dataFolder = dataFolder;
        }

        #endregion

        #region PRIVATE VARIABLES
        private Index<Meta2DataFormat> _si;
        private string _dataFolder;
        private int discarded = 0;
        public const int SAMPLERATE = 25;
        private const int NUMSIGNCHANGE = 50;
        private const int SHIFT = 5;
        #endregion

    }

    class InsectDataLoader : DataLoader
    {
        #region PUBLIC METHODS

        public override void LoadIndex()
        {
            ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
            SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));

            double[] dataBuffer;
            IDataFormat dl;
            double[] ts;
            int discarded = 0;

            string[] files = Directory.GetFiles(_dataDir, "*.dat");
            Array.Sort(files, new NaturalStringComparer());

            if (files.Length != NUMFILES)
                throw new ApplicationException("numFiles != NUMFILES");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("File => Number Mapping");

            for (int i = 0; i < NUMFILES; ++i)
            {
                string file = files[i];
                sb.AppendFormat("{0} => {1}", Path.GetFileNameWithoutExtension(file), i);
                Console.WriteLine("Processed:{2} Discarded:{0} AtFile:{1}", discarded, file, processed);

                if (!File.Exists(file))
                    throw new ApplicationException("!File.Exists(file)");

                // read data file into memory
                using (BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 100000000)))
                {
                    if (Math.IEEERemainder(br.BaseStream.Length, sizeof(double)) != 0)
                        throw new ApplicationException("Math.IEEERemainder( br.BaseStream.Length, sizeof(double)) != 0");

                    dataBuffer = new double[br.BaseStream.Length / sizeof(double)];
                    int offset = 0;

                    for (int pos = 0; pos < br.BaseStream.Length; pos += sizeof(double))
                        dataBuffer[offset++] = br.ReadDouble();
                }

                // sliding window and extract time series subsequences

                for (int pos = 0; pos < dataBuffer.Length - Globals.TimeSeriesLength; ++pos)
                {
                    ts = new double[Globals.TimeSeriesLength];
                    Array.Copy(dataBuffer, pos, ts, 0, Globals.TimeSeriesLength);

                    // filter
                    double std = Util.StdDev(ts);
                    if (std <= FILTERVAL)
                    {
                        discarded += (int)Math.Ceiling(Globals.TimeSeriesLength / 2.0) + 1;
                        pos += (int)Math.Ceiling(Globals.TimeSeriesLength / 2.0);
                        continue;
                    }
                    else
                    {
                        // normalize
                        double mean = Util.Mean(ts, 0, ts.Length - 1);
                        for (int j = 0; j < ts.Length; ++j)
                            ts[j] = (ts[j] - mean) / std;

                        dl = new Meta3DataFormat(i, pos, ts);
                        _si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(ts, opts)));
                        processed++;

                        if (processed % Globals.FlushTsVal == 0)
                            _si.FlushEntries();
                    }
                }

                GC.Collect();
            }
            _si.FlushEntries();

            Console.WriteLine();
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Console.WriteLine("Processed:{0} {1}", processed, _si.NumTimeSeries);
            Console.WriteLine("Discarded:{0}", discarded);
            Console.WriteLine();
        }

        #endregion

        #region CLASS CONSTRUCTORS
        public InsectDataLoader(Index<Meta3DataFormat> si,
            string dataDir)
        {
            this._si = si;
            this._dataDir = dataDir;
        }
        #endregion

        #region PRIVATE VARIABLES
        private Index<Meta3DataFormat> _si;
        private string _dataDir;
        private const int NUMFILES = 36;
        private const double FILTERVAL = 0.06;
        #endregion
    }

    #region http://stackoverflow.com/questions/248603/natural-sort-order-in-c
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }

    public sealed class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a, b);
        }
    }
    #endregion
}