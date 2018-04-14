using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
namespace SaxIndex
{
    class FromFileRawShapeletLoader : DataLoader
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
                        double[] line_data = Util.StringToArray(line);
                        //double ts_class = line_data.First();
                        double[] ts = Util.NormalizationHandler(line_data.Skip(1).ToArray());
                        //double[] together = new double[ts.Length + 1];
                        //together[0] = ts_class;
                        ts.CopyTo(line_data, 1);
                        if (!this.tsLength.HasValue)
                            this.tsLength = (uint)ts.Length;
                        else
                            if (this.tsLength.Value != ts.Length)
                            throw new ApplicationException("Inconsistent length when reading from file.");

                        this.buffer.Enqueue(line_data);
                        if (this.buffer.Count == this.bufferSize)
                            break;
                    }
                    if (line == null)
                        this.allRead = true;

                }
                else
                {
                    double[] tmp = this.buffer.Dequeue();
                    double[] ts = tmp.Skip(1).ToArray();
                    double shapelet_ts = tmp.First();
                    IDataFormat dl = new RawShapeletFormat(ts, shapelet_ts);
                    this.si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(ts, opts)));
                    this.processed++;
                }
            }

            this.sr.Close();
            //this.si.ForceFlushBuffers();
            this.si.FlushEntries();
            Console.WriteLine("Total: {0}", this.processed);
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public FromFileRawShapeletLoader(Index<RawShapeletFormat> si,
            string dataFile, int bufferSize)
        {
            this.si = si;
            this.dataFile = dataFile;
            this.bufferSize = bufferSize;
            this.buffer = new Queue<double[]>(this.bufferSize);
        }

        #endregion

        #region PRIVATE VARIABLES

        private Index<RawShapeletFormat> si;
        private StreamReader sr = null;
        private int bufferSize;
        private Queue<double[]> buffer;
        private string dataFile;
        private bool allRead = false;
        private uint? tsLength = null;
        #endregion
    }
}
