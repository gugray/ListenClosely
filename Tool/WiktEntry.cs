using System;
using System.Collections.Generic;
using System.IO;

namespace Tool
{
    public class WiktEntry
    {
        public string Lemma = null;
        public string PoS = "";
        public string Pron = "";
        public string Lemmatized = "";
        public string Details = "";
        public List<string> Meanings = new List<string>();
        public List<string> Translations = new List<string>();

        public void WriteRu(StreamWriter sw)
        {
            sw.WriteLine(Lemma + "\t" + PoS + "\t" + Pron + "\t" + Lemmatized + "\t" + Details);
            foreach (var x in Meanings) sw.WriteLine(x);
            foreach (var x in Translations) sw.WriteLine(x);
            sw.WriteLine();
        }

        public static WiktEntry FromLinesDe(List<string> lines)
        {
            string[] parts = lines[0].Split('\t');
            WiktEntry res = new WiktEntry
            {
                Lemma = parts[0],
                PoS = parts[1],
            };
            bool inTranslations = false;
            for (int i = 1; i < lines.Count; ++i)
            {
                string ln = lines[i];
                if (!ln.StartsWith(":")) inTranslations = true;
                if (ln.StartsWith("::")) continue;
                if (!inTranslations) res.Meanings.Add(ln);
                else res.Translations.Add(ln);
            }
            return res;
        }

        public static WiktEntry FromLinesRu(List<string> lines)
        {
            string[] parts = lines[0].Split('\t');
            WiktEntry res = new WiktEntry
            {
                Lemma = parts[0],
                PoS = parts[1],
            };
            if (parts.Length > 2) res.Pron = parts[2];
            if (parts.Length > 3) res.Lemmatized = parts[3];
            if (parts.Length > 4) res.Details = parts[4];
            bool inTranslations = false;
            for (int i = 1; i < lines.Count; ++i)
            {
                string ln = lines[i];
                if (!ln.StartsWith("#")) inTranslations = true;
                if (!inTranslations) res.Meanings.Add(ln);
                else res.Translations.Add(ln);
            }
            return res;
        }
    }
}
