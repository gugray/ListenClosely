using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tool
{
    class Utils
    {
        static Regex reSec = new Regex(@"([\d]+):([\d]+):([\d]+)[:,]([\d]+)");

        public static decimal ParseSec(string str)
        {
            var m = reSec.Match(str);
            string fracStr = m.Groups[4].Value;
            if (fracStr.Length == 2) fracStr += "0";
            decimal sec = decimal.Parse(m.Groups[1].Value) * 60 * 60 +
                decimal.Parse(m.Groups[2].Value) * 60 +
                decimal.Parse(m.Groups[3].Value) +
                decimal.Parse(fracStr) / 1000;
            return sec;
        }

        public static string WriteSec(decimal val)
        {
            decimal x = Math.Floor(val);
            decimal msec = 1000 * (val - x);
            decimal y = Math.Floor(x / 60) * 60;
            decimal sec = x - y;
            y /= 60;
            decimal z = Math.Floor(y / 60) * 60;
            decimal min = y - z;
            z /= 60;
            decimal t = Math.Floor(z / 60) * 60;
            decimal hour = z - t;
            return hour.ToString("00") + ":" + min.ToString("00") + ":" + sec.ToString("00") + "," + msec.ToString("000");
        }
    }
}
