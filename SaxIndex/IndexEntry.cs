using System;
using System.Collections.Generic;
using System.Text;
namespace SaxIndex
{

    [Serializable]
    public abstract class IndexEntry
    {
        #region PUBLIC METHODS

        //public abstract void FlushBufferToDisk();


        public abstract override string ToString();


        #endregion // PUBLIC METHODS

        #region PUBLIC PROPERTIES

        public abstract int NumNodes
        {
            get;
        }

        public abstract int NumTimeSeries
        {
            get;
        }

        public abstract string SaxWord
        {
            get;
        }

        #endregion // PUBLIC PROPERTIES

    }

}

