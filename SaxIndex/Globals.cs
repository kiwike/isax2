using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    /// <summary>
    /// Global values used across the program
    /// </summary>
    public static class Globals
    {
        public static int TimeSeriesLength { get; private set; }
        public static byte SaxWordLength { get; private set; }
        public static ushort SaxMaxCard { get; private set; }
        public static ushort SaxBaseCard { get; private set; }
        public static string IndexRootDir { get; private set; }
        public static int IndexNumMaxEntries { get; private set; }
        public static int FlushTsVal { get; private set; }
        public static Boolean NewSplitPolicy { get; private set; }

        // Computed from other variables
        public static int TimeSeriesByteLength { get; private set; }
        public static int SaxValuesByteLength { get; private set; }

        public static void Initalize(
            int timeSeriesLength,
            byte saxWordLength,
            ushort saxMaxCard,
            ushort saxBaseCard,
            string  indexRootDir,
            int indexNumMaxEntries,
            int flushTsVal,
            Boolean newsplitpolicy
            )
        {
            TimeSeriesLength = timeSeriesLength;
            SaxWordLength = saxWordLength;
            SaxMaxCard = saxMaxCard;
            SaxBaseCard = saxBaseCard;
            IndexRootDir = indexRootDir;
            IndexNumMaxEntries = indexNumMaxEntries;
            FlushTsVal = flushTsVal;
            NewSplitPolicy = newsplitpolicy;

            SaxValuesByteLength = SaxWordLength * sizeof(ushort);
            TimeSeriesByteLength = TimeSeriesLength * sizeof(double);
        }

    }
}
