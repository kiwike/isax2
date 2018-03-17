using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace SaxIndex
{
    [Serializable]
    public class SaxOptions
    {
        #region PUBLIC METHODS

        public override bool Equals(object obj)
        {
            return this == (SaxOptions)obj;
        }

        public override int GetHashCode()
        {
            // 
            return base.GetHashCode();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion // PUBLIC METHODS

        #region CLASS CONSTRUCTORS

        public SaxOptions(ushort[] newMask)
        {
            this.mask = new ReadOnlyCollection<ushort>(newMask);
        }
        #endregion // CLASS CONSTRUCTORS

        #region PUBLIC PROPERTIES

        public ushort[] MaskCopy
        {
            get
            {
                ushort[] tmp = new ushort[Globals.SaxWordLength];
                this.mask.CopyTo(tmp, 0);
                return tmp;
            }
        }

        public ReadOnlyCollection<ushort> Mask
        {
            get
            {
                return this.mask;
            }
        }

        #endregion // PUBLIC PROPERTIES

        #region PRIVATE VARIABLES

        private readonly ReadOnlyCollection<ushort> mask;
        #endregion // PRIVATE VARIABLES

        #region STATIC METHODS

        public static bool operator !=(SaxOptions l, SaxOptions r)
        {
            return !(l == r);
        }


        public static bool operator ==(SaxOptions l, SaxOptions r)
        {
            for (int i = 0; i < Globals.SaxWordLength; i++)
            {
                if (l.mask[i] != r.mask[i])
                    return false;
            }
            return true;
        }
        #endregion
    }
}
