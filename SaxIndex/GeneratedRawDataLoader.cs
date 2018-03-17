using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SaxIndex
{
    public class GeneratedRawDataLoader : DataLoader
    {
        #region PUBLIC METHODS

        
        public override void LoadIndex()
        {
           Util.SeedGenerator(seed);
           ushort maskval = (ushort)(Math.Log(Globals.SaxMaxCard, 2) - Math.Log(Globals.SaxBaseCard, 2));
           SaxOptions opts = new SaxOptions(Util.UnsignedShortArray(Globals.SaxWordLength, maskval));
           
           double[] ts; 
           IDataFormat dl;
           while (this.processed < this.numTs)            
            {
                ts = Util.RandomWalk(this.tsLength);
                dl = new RawDataFormat(ts);
                this.si.Insert(new SaxData(dl, Sax.ArrayToSaxVals(ts, opts)));  // Continuesly insertion on the first level of buffers ( with no threshold )
                this.processed++;
                Console.Write("\r{0}", this.processed);
                if (this.processed % Globals.FlushTsVal== 0) {     // When reachs the value flush on disk
                    this.si.FlushEntries();
                }
            }
           this.si.FlushEntries();
           Console.WriteLine();
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public GeneratedRawDataLoader(Index<RawDataFormat> si, int tsLength, int numTs , int seed)
        {
            this.seed = seed;
            this.si = si;
            this.tsLength = tsLength;
            this.numTs = numTs;
        }

        #endregion

        #region PRIVATE VARIABLES

        private Index<RawDataFormat> si;
        readonly int tsLength;
        readonly int numTs;
        readonly int seed;

        #endregion

    }
}
