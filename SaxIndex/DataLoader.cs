using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
namespace SaxIndex
{
    // load data between a repository and index, from file, generated, etc...
    public abstract class DataLoader
    {

        #region PUBLIC METHODS

        public abstract void LoadIndex();

        #endregion

        #region PUBLIC PROPERTIES

        public uint Processed
        {
            get
            {
                return this.processed;
            }
        }

        #endregion

        #region PROTECTED VARIABLES

        protected uint processed;

        #endregion

    }
}
