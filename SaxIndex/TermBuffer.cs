using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SaxIndex
{
    public struct Buf                      // Single Buffer Structure
    {
        public int Utilization;            // Utilization of the Buffer
        public TermEntry Node;             // Node of the buffer
        public List<SaxData> BL;        // List of TS in the buffer

        public void Initialization()
        {
            BL = new List<SaxData>();
            Utilization = 0;
            Node = null;
        }

        public void InsertInBuffer(SaxData entry)
        {
            if (Utilization >= TermBuffer.SingleBufferSize)
            {  // Check if the TermNode can not fit other TS
                FlushBuffer();
            }
            BL.Add(entry);
            Utilization++;
        }

        public void FlushBuffer()
        {
            Node.FlushBufferToDisk(BL);          // Flush on Disk
            BL.Clear();                          // Clear the List
            Node.NBuf = -1;                      // Delete the Association with the Node 
            Utilization = 0;                     // Reset the Utilization
        }
        
        public List<SaxData> getbuffer()
        {
            return BL;
        }

        public void setnode(TermEntry node) {
            Node = node;
        }
    }

    static class TermBuffer                // Buffers Array Structure
    {
        public static int SingleBufferSize { get; private set; }      // Length of the Single Buffer
        private static List<Buf> TBuffer = new List<Buf>();

        public static void Initialize(int singleBufferSize)
        {
            SingleBufferSize = singleBufferSize;
            TBuffer = new List<Buf>();
        }

        public static void InsertInBuffer(SaxData entry, int i)
        {
            TBuffer[i].InsertInBuffer(entry);
        }

        public static int CreateNewBuffer(SaxData entry, TermEntry node)
        {
            Buf B = new Buf();
            B.Initialization();
            B.setnode(node);
            B.InsertInBuffer(entry);   // Insert the Time series in the new Buffer
            TBuffer.Add(B);
            return (TBuffer.Count - 1);
        }

        public static void ForceFlushBuffer(int N)
        {
            if (N != -1)  // If the Buffer exist, flush it
            {
                TBuffer[N].FlushBuffer();
            }

        }

        public static void FinishInsertions()  //Flush All the buffers on Disk
        {
            for (int i = 0; i < TBuffer.Count; i++)
            {
                if (TBuffer[i].Node != null && TBuffer[i].Utilization > 0)
                    TBuffer[i].FlushBuffer();
            }
            TBuffer = new List<Buf>();
        }

        public static List<SaxData> getbuffer(int N)
        {
            List<SaxData> B = null;
            if (N != -1)  
            {
                B = TBuffer[N].getbuffer();
                Buf tmp = new Buf();
                tmp.Initialization();
                TBuffer[N] = tmp;
            }
            return B;
        }

    }

}
