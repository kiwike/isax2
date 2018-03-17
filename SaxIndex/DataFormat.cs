using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    public interface IDataFormat
    {
        void InitFromByteArray(byte[] b);
        byte[] ToByteArray();
        double[] GetTimeSeries();
    }

    // data directly
    public struct RawDataFormat : IDataFormat
    {
        public static int ByteLength
        {
            get { return Globals.TimeSeriesByteLength; }
        }

        public RawDataFormat(double[] data)
        {
            this.data = data;
        }

        #region IDataLocation Members

        public void InitFromByteArray(byte[] b)
        {
            data = Util.ByteArrayToDoubleArray(b);
        }

        public byte[] ToByteArray()
        {
            return Util.DoubleArrayToByteArray(data);
        }

        public double[] GetTimeSeries()
        {
            return data;
        }

        #endregion

        private double[] data;
    }

    public struct Meta3DataFormat : IDataFormat
    {
        public static int ByteLength
        {
            get { return Globals.TimeSeriesByteLength + 2 * sizeof(int); }
        }

        #region IDataLocation Members

        public Meta3DataFormat(int meta1, int meta2, double[] data)
        {
            this.meta1 = meta1;
            this.meta2 = meta2;
            this.data = data;
        }

        public void InitFromByteArray(byte[] b)
        {
            meta1 = BitConverter.ToInt32(b, 0);
            meta2 = BitConverter.ToInt32(b, sizeof(int));
            int numBytes = b.Length - 2 * sizeof(int);
            data = new double[numBytes / sizeof(double)];
            Buffer.BlockCopy(b, b.Length - numBytes, data, 0, numBytes);
        }

        public byte[] ToByteArray()
        {
            byte[] b = new byte[2 * sizeof(int) + sizeof(double) * data.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(meta1), 0, b, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(meta2), 0, b, sizeof(int), sizeof(int));
            Buffer.BlockCopy(data, 0, b, 2 * sizeof(int), sizeof(double) * data.Length);
            return b;
        }

        public double[] GetTimeSeries()
        {
            return data;
        }

        #endregion

        public int meta1;
        public int meta2;
        private double[] data;
    }

    public struct Meta2DataFormat : IDataFormat
    {
        public static int ByteLength
        {
            get { return 2 * sizeof(int); }
        }

        #region IDataLocation Members

        public Meta2DataFormat(int meta1, int meta2)
        {
            this._chrNo = meta1;
            this._pos = meta2;
        }

        public void InitFromByteArray(byte[] b)
        {
            _chrNo = BitConverter.ToInt32(b, 0);
            _pos = BitConverter.ToInt32(b, sizeof(int));

        }

        public byte[] ToByteArray()
        {
            byte[] b = new byte[2 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(_chrNo), 0, b, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(_pos), 0, b, sizeof(int), sizeof(int));
            return b;
        }

        // !! not normalized
        public double[] GetTimeSeries()
        {
            if (tsBuf == null)
                tsBuf = new double[Globals.TimeSeriesLength];
            for (int i = 0; i < tsBuf.Length; ++i)
                tsBuf[i] = dnaBuffer[_chrNo][i + _pos];

            return tsBuf;
        }

        #endregion

        public int _chrNo;
        public int _pos;
        public static List<double[]> dnaBuffer;
        public static double[] tsBuf;
    }

    public struct Meta1DataFormat : IDataFormat
    {
        public static int ByteLength
        {
            get { return Globals.TimeSeriesByteLength + 1 * sizeof(long); }
        }

        #region IDataLocation Members

        public Meta1DataFormat(long meta, double[] data)
        {
            this.meta = meta;
            this.data = data;
        }

        public void InitFromByteArray(byte[] b)
        {
            meta = BitConverter.ToInt64(b, 0);
            int numBytes = b.Length - sizeof(long);
            data = new double[numBytes / sizeof(double)];
            Buffer.BlockCopy(b, b.Length - numBytes, data, 0, numBytes);
        }

        public byte[] ToByteArray()
        {
            byte[] b = new byte[sizeof(long) + sizeof(double) * data.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(meta), 0, b, 0, sizeof(long));
            Buffer.BlockCopy(data, 0, b, sizeof(long), sizeof(double) * data.Length);
            return b;
        }

        public double[] GetTimeSeries()
        {
            return data;
        }

        #endregion

        public long meta;
        private double[] data;
    }
}