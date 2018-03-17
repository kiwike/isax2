using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace SaxIndex
{
    [Serializable]
    public class SplitEntry<DLFormat> : IndexEntry where DLFormat : IDataFormat, new()
    {

        #region PUBLIC VARIABLES

        #endregion

        #region PUBLIC METHODS
        public TermEntry ApproximateSearch(SaxData dr)
        {
            return index.ApproximateSearch(dr);
        }

        public Dictionary<string, IndexEntry>.ValueCollection GetIndexEntries()
        {
            return index.GetIndexEntries();
        }

        public void Insert(SaxData dr) // Insertion for Splitnodes not in the first level of the index
        {
            index.Insert(dr);
        }

        public override string ToString()
        {
            return Path.Combine(Path.GetFileNameWithoutExtension(index.WorkingFolder), SaxWord);
        }

        #endregion // PUBLIC METHODS

        #region CLASS CONSTRUCTORS

        public SplitEntry(string saxWord, IndexOptions options, byte splitDepth)
        {
            this.saxWord = saxWord;
            this.index = new Index<DLFormat>(splitDepth, options);
        }

        #endregion // CLASS CONSTRUCTORS

        #region PUBLIC PROPERTIES

        public override int NumNodes
        {
            get
            {
                return 1 + index.NumNodes;
            }
        }

        public override int NumTimeSeries
        {
            get
            {
                return index.NumTimeSeries;
            }
        }

        public IndexOptions Options
        {
            get
            {
                return index.Options;
            }
        }

        public override string SaxWord
        {
            get
            {
                return saxWord;
            }
        }

        #endregion // PUBLIC PROPERTIES

        #region PRIVATE VARIABLES

        private Index<DLFormat> index = null;
        private string saxWord;
        
        #endregion // PRIVATE VARIABLES
    }
}

