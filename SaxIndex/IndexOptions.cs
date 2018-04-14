using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace SaxIndex
{

    [Serializable]
    public class IndexOptions
    {
        #region PUBLIC METHODS

        public override bool Equals(object obj)
        {
            IndexOptions tmp = (IndexOptions)obj;
            if (tmp.saxOpts != this.saxOpts || tmp.baseDir != this.baseDir)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();//update
        }

        public String maskValue()
        {
            String maskValue = "";
            foreach (ushort m in this.SaxOpts.Mask)
            {
                maskValue = maskValue + m.ToString() + " ";
            }
            maskValue = maskValue.Substring(0, maskValue.Length - 1);
            return maskValue;
        }

        #endregion

        #region CLASS CONSTRUCTORS

        public IndexOptions(
                   string baseFolder,
                   ushort[] newMask)
        {
            this.saxOpts = new SaxOptions(newMask);
            this.baseDir = baseFolder;
        }

        public IndexOptions(SaxOptions saxOpts, string baseFolder)
        {
            this.baseDir = baseFolder;
            this.saxOpts = saxOpts;
        }

        public IndexOptions(string baseFolder)
            : this(
                        baseFolder,
                        Util.UnsignedShortArray(Globals.SaxWordLength, 0))
        {
        }

        #endregion

        #region PUBLIC PROPERTIES

        public SaxOptions SaxOpts
        {
            get
            {
                return this.saxOpts;
            }
        }

        public ReadOnlyCollection<ushort> Mask
        {
            get
            {
                return this.saxOpts.Mask;
            }
        }


        public ushort[] MaskCopy
        {
            get
            {
                return this.saxOpts.MaskCopy;
            }
        }

        public string BaseDir
        {
            get
            {
                return baseDir;
            }
        }

        #endregion

        #region PRIVATE VARIABLES

        private readonly SaxOptions saxOpts;
        private string baseDir;

        #endregion

        #region STATIC METHODS

        public static bool operator ==(IndexOptions l, IndexOptions r)
        {
            return (l.baseDir == r.baseDir && l.saxOpts == r.saxOpts) ? true : false;
        }


        public static bool operator !=(IndexOptions l, IndexOptions r)
        {
            return !(l == r);
        }
        #endregion
    }

}

