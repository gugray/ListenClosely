using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;

namespace Tool
{
    class ReviewGenerator
    {
        readonly string kReview;
        readonly string kContext;
        readonly string kEntry;

        public ReviewGenerator()
        {
            kReview = readStringResource("review.html");
            kContext = readStringResource("context.html");
            kEntry = readStringResource("entry.html");
        }

        public void Print(Material mat, string fnHtml)
        {
            StringBuilder sbMain = new StringBuilder();
            foreach (var segm in mat.Segments)
                sbMain.Append(doSegment(segm, mat.DictEntries));
            string html = kReview.Replace("{{main}}", sbMain.ToString());
            File.WriteAllText(fnHtml, html);
        }

        string doSegment(Segment segm, List<DictEntry> dictEntries)
        {
            StringBuilder sbSegm = new StringBuilder();
            for (int i = 0; i < segm.Words.Count; ++i)
            {
                string sentenceHtml = buildContextSentenceHtml(segm, i);
                string entriesHtml = buildEntriesHtml(segm.Words[i], dictEntries);
                string context = kContext.Replace("{{sentence}}", sentenceHtml);
                context = context.Replace("{{entries}}", entriesHtml);
                // Store context with annotations
                sbSegm.Append(context);
            }
            return sbSegm.ToString();
        }

        string buildEntriesHtml(Word word, List<DictEntry> dictEntries)
        {
            StringBuilder sbAllEntries = new StringBuilder();
            foreach (var eix in word.Entries)
            {
                DictEntry de = dictEntries[eix];
                StringBuilder sbSenses = new StringBuilder();
                foreach (var sense in de.Senses)
                {
                    sbSenses.Append("<li>");
                    sbSenses.Append(esc(sense.SrcDef));
                    sbSenses.Append("</li>");
                }
                string entryHtml = kEntry.Replace("{{senses}}", sbSenses.ToString());
                entryHtml = entryHtml.Replace("{{displayHead}}", esc(de.DisplayHead));
                sbAllEntries.Append(entryHtml);
            }
            return sbAllEntries.ToString();
        }

        string buildContextSentenceHtml(Segment segm, int wordIx)
        {
            StringBuilder sbSent = new StringBuilder();
            for (int j = 0; j < wordIx; ++j)
            {
                if (sbSent.Length > 0) sbSent.Append(' ');
                sbSent.Append(esc(segm.Words[j].Lead));
                sbSent.Append(esc(segm.Words[j].Text));
                sbSent.Append(esc(segm.Words[j].Trail));
            }
            if (sbSent.Length > 0) sbSent.Append(' ');
            sbSent.Append(esc(segm.Words[wordIx].Lead));
            sbSent.Append("<span>");
            sbSent.Append(esc(segm.Words[wordIx].Text));
            sbSent.Append("</span>");
            sbSent.Append(esc(segm.Words[wordIx].Trail));
            for (int j = wordIx + 1; j < segm.Words.Count; ++j)
            {
                if (sbSent.Length > 0) sbSent.Append(' ');
                sbSent.Append(esc(segm.Words[j].Lead));
                sbSent.Append(esc(segm.Words[j].Text));
                sbSent.Append(esc(segm.Words[j].Trail));
            }
            return sbSent.ToString();
        }

        string readStringResource(string fn)
        {
            var ass = Assembly.GetExecutingAssembly();
            using (Stream s = ass.GetManifestResourceStream("Tool.Resources." + fn))
            using (StreamReader rdr = new StreamReader(s))
            {
                return rdr.ReadToEnd();
            }
        }

        static string esc(string str)
        {
            str = str.Replace("&", "&amp;");
            str = str.Replace("<", "&lt;");
            str = str.Replace(">", "&gt;");
            return str;
        }
    }
}