using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    public struct CostCounter
    {
        public int IO;
        public int distance;

        public CostCounter(int io, int dist)
        {
            this.IO = io;
            this.distance = dist;
        }

        public static CostCounter operator +(CostCounter l, CostCounter r)
        {
            return new CostCounter(l.IO + r.IO, l.distance + r.distance);
        }

        public override string ToString()
        {
            return string.Format("IO:{0}  distance:{1}", IO,  distance);
        }

    }
}
