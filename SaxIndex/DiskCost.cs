using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    class DiskCost
    {
        public static long seqcost = 0;
        public static long rancost = 0;
        public static long readcost = 0;
        public static long savedcost = 0;

        public static void increasereadcost()
        {
            readcost++;
        }

        public static void increasesequentialcost()
        {
            seqcost++;
        }

        public static void increaserandomcost()
        {
            rancost++;
        }

        public static void increasesavedcost(int i)
        {
            savedcost += i;
        }
    }
}

