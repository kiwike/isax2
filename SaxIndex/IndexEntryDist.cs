using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    [Serializable]
    public struct IndexEntryDist : IComparable<IndexEntryDist>
    {
        public IndexEntry entry;
        public double dist;
        public IndexEntryDist(IndexEntry e, double d)
        {
            if (e == null)
                throw new ApplicationException("IndexEntry is not instantiated.");
            this.dist = d;
            this.entry = e;
        }

        #region IComparable<EntryInfo> Members
        int IComparable<IndexEntryDist>.CompareTo(IndexEntryDist other)
        {
            return this.dist.CompareTo(other.dist);
        }
        #endregion
    }
}
