using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;

namespace Tool
{
    class CsvToSrt
    {
        public static void ConvertFile(string fnCsv, string fnSrt)
        {
            int seq = 0;
            using (StreamReader sr = new StreamReader(fnCsv))
            using (CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture))
            using (StreamWriter sw = new StreamWriter(fnSrt))
            {
                sw.NewLine = "\n";
                csv.Configuration.Delimiter = ";";
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    ++seq;
                    decimal startSec = Utils.ParseSec(csv.GetField("Start"));
                    decimal lengthSec = Utils.ParseSec(csv.GetField("Duration"));
                    string text = csv.GetField("Transcript");
                    sw.WriteLine(seq.ToString());
                    sw.WriteLine(Utils.WriteSec(startSec) + " --> " + Utils.WriteSec(startSec + lengthSec));
                    sw.WriteLine(text);
                    sw.WriteLine();
                }
            }
        }
    }
}
