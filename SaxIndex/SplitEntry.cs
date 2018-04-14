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

        public Index<DLFormat> GetIndex() {
            return index;
        }

        public override string ToString()
        {
            return Path.Combine(Path.GetFileNameWithoutExtension(index.WorkingFolder), SaxWord);
        }

        public string GetiSaxWord()
        {
            String[] saxWord = this.saxWord.Split('_');
            String[] maskValue = index.Options.maskValue().Split(' ');
            String iSaxWord = "";
            if (saxWord.Length == maskValue.Length)
            {
                for (int i = 0; i < saxWord.Length; i++)
                {
                    iSaxWord = iSaxWord + saxWord[i] + "." + maskValue[i] + " ";
                }
                iSaxWord = iSaxWord.Substring(0, iSaxWord.Length - 1);
            }
            return iSaxWord;
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

        public string iSaxWord
        {
            get
            {
                return GetiSaxWord();
            }
        }

        #endregion // PUBLIC PROPERTIES

        #region PRIVATE VARIABLES

        private Index<DLFormat> index = null;
        private string saxWord;
        
        #endregion // PRIVATE VARIABLES
    }
}

