using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SaxIndex
{
    [Serializable]
    public class TermEntry : IndexEntry
    {

        #region PUBLIC METHODS

        public void InsertToBuffer(SaxData dr)
        {
            numTimeSeries++;
            if (NBuf == -1)             // Check if there is a buffer attached to this node
            {
                NBuf = TermBuffer.CreateNewBuffer(dr, this);   // Create one and return the ID
            }
            else
            {
                TermBuffer.InsertInBuffer(dr, NBuf);   // If it exists insert ts in the buffer
            }
        }

        public void ForceFlushBuffer()
        {
            TermBuffer.ForceFlushBuffer(NBuf);
        }

        public List<SaxData> getbuffer()
        {
            return TermBuffer.getbuffer(NBuf);

        }

        public void FlushBufferToDisk(List<SaxData> buf)
        {
            FileStream fs;
            if (File.Exists(FileName))
            {
                fs = new FileStream(FileName, FileMode.Append);
            }
            else
            {
                fs = new FileStream(FileName, FileMode.Create);
            }

            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                onDisk = true;
                fileAccessCount++;           // Increment the number of file access everytime that write on disk a set of ts
                DiskCost.increaserandomcost();
                // Read all entries into memory buffer
                // memory stream ctor takes in buffer size, currently a poor estimate
                using (MemoryStream ms = new MemoryStream(buf.Count * (Globals.SaxValuesByteLength)))
                {
                    foreach (SaxData data in buf)
                    {
                        DiskCost.increasesequentialcost();
                        byte[] dataBytes = data.ToBytes();
                        ms.Write(dataBytes, 0, dataBytes.Length);
                    }
                    bw.Write(ms.ToArray());
                }
            }
            fs.Close();
        }

        public override string ToString()
        {
            return string.Concat(Path.GetFileNameWithoutExtension(Path.GetDirectoryName(fName)), SaxWord);
        }

        #endregion // PUBLIC METHODS

        #region CLASS CONSTRUCTORS

        private TermEntry(string fileName)
        {
            this.numTimeSeries = 0;
            this.fName = fileName;

        }
        public TermEntry(string saxWord, string fileName)
            : this(fileName) // debug
        {
            if (saxWord != TermEntry.FileNameParseSaxStr(fileName))
                throw new ApplicationException("TermEntry inconsistency saxWord does not match fileName.");
        }

        #endregion // CLASS CONSTRUCTORS

        #region PUBLIC PROPERTIES

        public string FileName
        {
            get
            {
                return fName;
            }
            set
            {
                this.fName = value;
            }
        }

        public override int NumNodes
        {
            get
            {
                return 1;
            }
        }

        public override int NumTimeSeries
        {
            get
            {
                return numTimeSeries;
            }
        }

        public bool OnDisk
        {
            get
            {
                return onDisk;
            }
        }

        public override string SaxWord
        {
            get
            {
                return TermEntry.FileNameParseSaxStr(fName);
            }
        }

        public string iSaxWord
        {
            get
            {
                return TermEntry.FileNameParseiSaxStr(fName);
            }
        }

        #endregion // PUBLIC PROPERTIES

        #region PUBLIC VARIABLES

        public int NBuf = -1;

        #endregion

        #region PRIVATE VARIABLES

        private string fName;
        private int numTimeSeries;
        private bool onDisk = false;    // true implies entries reside partially or completely on disk
        //private List<SaxDataRep> BufList = new List<SaxDataRep>();  What is this?

        #endregion // PRIVATE VARIABLES

        #region STATIC METHODS

        public static string FileNameParseSaxStr(string line) // deprecated
        {
            //return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(line));
            string saxstring = "";
            line = line.Substring(line.LastIndexOf("/")+1);
            for (int i = 0; i < Globals.SaxWordLength; i++) { 
                saxstring=saxstring+ line.Substring(0,line.IndexOf("."))+"_";
                line=line.Substring(line.IndexOf("_")+1);
            }
            saxstring = saxstring.Substring(0, saxstring.Length - 1);
            return saxstring;
        }

        public static string FileNameParseiSaxStr(string line) 
        {
            //return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(line));
            string isaxstring = "";
            line = line.Substring(line.LastIndexOf("/") + 1);
            string search = @"[0-9]+\.[0-9]+";
            MatchCollection matches = Regex.Matches(line, search);
            foreach (Match m in matches)
            {
                isaxstring = isaxstring + m.Value + " ";
            }
            /*for (int i = 0; i < Globals.SaxWordLength; i++)
            {
                isaxstring = isaxstring + line.Substring(0, line.IndexOf('.', line.IndexOf(".")+1)) + " ";
                line = line.Substring(line.IndexOf("_") + 1);
            }*/
            isaxstring = isaxstring.Substring(0, isaxstring.Length - 1);
            return isaxstring;
        }

        #endregion

        #region STATIC PROPERTIES

        #endregion

        #region STATIC VARIABLES

        public static int fileAccessCount = 0;    // Counter for the File accesses 

        #endregion

    }

}

