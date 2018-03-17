using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{

    [Serializable]
    public struct IndexFileDist : IComparable<IndexFileDist>
    {

        public IndexFileDist(string f, int l, double d)
        {
            if (l == 0)
                throw new ApplicationException("Line number cannot be 0.");

            fileName = f;
            lineNum = l;
            distance = d;
        }

        public override string ToString()
        {
            return string.Concat(fileName,
                Util.Delimiter.ToString(),
                lineNum.ToString(),
                Util.Delimiter.ToString(),
                distance.ToString());
        }


        public static bool operator ==(IndexFileDist l, IndexFileDist r)
        {
            if (l.lineNum == r.lineNum &&
                l.fileName == r.fileName &&
                Math.Abs((l.distance - r.distance)) < Util.Epsilon)
                return true;
            return false;
        }

        public static bool operator !=(IndexFileDist l, IndexFileDist r)
        {
            return !(l == r);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode(); //update
        }

        public override bool Equals(object obj)
        {
            return this == (IndexFileDist)obj;
        }

        // sort by dist          
        #region IComparable<IndexFileDist> Members

        int IComparable<IndexFileDist>.CompareTo(IndexFileDist other)
        {
            return this.distance.CompareTo(other.distance);
        }

        #endregion

        public string fileName;
        public int lineNum;
        public double distance;
    }
}
