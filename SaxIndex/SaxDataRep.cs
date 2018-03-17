using System;
using System.Collections.Generic;
using System.Text;

namespace SaxIndex
{
    // basic unit of data, holds precomputed sax values and location specification
    [Serializable]
    public struct SaxData
    {
        public IDataFormat dl;
        public ushort[] values;

        public SaxData(IDataFormat dl, ushort[] values)
        {
            this.dl = dl;
            this.values = (ushort[])values.Clone();
        }

        public SaxData(ushort[] values)
        {
            this.dl = null;
            this.values = (ushort[])values.Clone();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
            //return String.Format("{0}{1}{2}", Sax.SaxValsToSaxStr(values),
            //    Util.Delimiter, dl.DataToByteArray());
        }

        public byte[] ToBytes()
        {
            tmpDataLocBytes = dl.ToByteArray();
            byte[] b = new byte[tmpDataLocBytes.Length + Globals.SaxValuesByteLength];

            System.Buffer.BlockCopy(this.values, 0, b, 0, Globals.SaxValuesByteLength);
            System.Buffer.BlockCopy(tmpDataLocBytes, 0, b, Globals.SaxValuesByteLength, tmpDataLocBytes.Length);
            return b;
        }

        public static SaxData Parse<DATAFORMAT>(byte[] b)
            where DATAFORMAT : IDataFormat, new()
        {
            tmpValues = new ushort[Globals.SaxWordLength];
            Buffer.BlockCopy(b, 0, tmpValues, 0, Globals.SaxValuesByteLength);

            tmpDataLocBytes = new byte[b.Length - Globals.SaxValuesByteLength];
            Buffer.BlockCopy(b, Globals.SaxValuesByteLength, tmpDataLocBytes, 0, b.Length - Globals.SaxValuesByteLength);

            DATAFORMAT dl = new DATAFORMAT();
            dl.InitFromByteArray(tmpDataLocBytes);
            return new SaxData(dl, tmpValues);
        }

        public static int ByteLength(Type dataFormatType)
        {
            int val = Globals.SaxValuesByteLength;
            if (dataFormatType == typeof(RawDataFormat))
            {
                val += RawDataFormat.ByteLength;
            }
            else if (dataFormatType == typeof(Meta1DataFormat))
            {
                val += Meta1DataFormat.ByteLength;
            }
            else if (dataFormatType == typeof(Meta2DataFormat))
            {
                val += Meta2DataFormat.ByteLength;
            }
            else if (dataFormatType == typeof(Meta3DataFormat))
            {
                val += Meta3DataFormat.ByteLength;
            }
            else
            {
                throw new NotImplementedException("Invalid FormatType");
            }
            return val;
        }

        private static ushort[] tmpValues;
        private static byte[] tmpDataLocBytes;
    }

}
