using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace WiktionaryParser
{
    class Program
    {
        static void dumpToEntries(string fnDump, string fnOut, dynamic parser)
        {
            Console.WriteLine("Parsing Wiktionary dump...");
            int totalPageCount = 0;
            int pagesWithEntry = 0;
            int entryCount = 0;
            using (var dpr = new DumpPageReader(fnDump))
            using (var sw = new StreamWriter(fnOut))
            {
                sw.NewLine = "\n";
                DumpPage page;
                while ((page = dpr.GetNextPage()) != null)
                {
                    ++totalPageCount;
                    List<WiktEntry> entries = parser.GetEntries(page.Title, page.Text);
                    if (entries.Count != 0)
                        ++pagesWithEntry;
                    entryCount += entries.Count;
                    foreach (var entry in entries) entry.WriteRu(sw);
                    //// DBG
                    //if (entryCount > 50000) break;
                }
            }
            Console.WriteLine(totalPageCount + " pages / " + pagesWithEntry + " word pages in language / " + entryCount + " entries");
        }

        static void entriesToDictDe(string fnEntries, string fnJson)
        {
            List<Entry> entries = new List<Entry>();
            var xformer = new WiktEntryTransformerDe();
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnEntries))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "" && entryLines.Count > 0)
                    {
                        var we = WiktEntry.FromLinesDe(entryLines);
                        if (we != null)
                        {
                            var xfe = xformer.Transform(we);
                            if (xfe != null && xfe.Head != "") entries.Add(xfe);
                        }
                        entryLines.Clear();
                        continue;
                    }
                    entryLines.Add(line);
                }
            }
            using (var sw = new StreamWriter(fnJson))
            {
                sw.NewLine = "\n";
                var allEntriesStr = JsonConvert.SerializeObject(entries, Formatting.Indented);
                sw.WriteLine(allEntriesStr);
            }
        }

        static void getHead(string fnIn, string fnOut, int lineCount)
        {
            using (StreamReader sr = new StreamReader(fnIn))
            using (StreamWriter sw = new StreamWriter(fnOut))
            {
                sw.NewLine = "\n";
                string line;
                int cnt = 0;
                while (cnt < lineCount && (line = sr.ReadLine()) != null)
                {
                    sw.WriteLine(line);
                    ++cnt;
                }
            }
        }

        static void getMarkupRu(string fnEntries, string fnBaseOut)
        {
            var poss = new Dictionary<string, int>();
            var meaningCurly = new Dictionary<string, int>();
            var enCurly = new Dictionary<string, int>();
            var deCurly = new Dictionary<string, int>();
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnEntries))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "" && entryLines.Count > 0)
                    {
                        var we = WiktEntry.FromLinesRu(entryLines);
                        if (we != null)
                        {
                            getMarkupRu(we, poss, meaningCurly, enCurly, deCurly);
                        }
                        entryLines.Clear();
                        continue;
                    }
                    entryLines.Add(line);
                }
            }
            writeMarkupCounts(poss, fnBaseOut + "-curly-poss.txt");
            writeMarkupCounts(meaningCurly, fnBaseOut + "-curly-meanings.txt");
            writeMarkupCounts(enCurly, fnBaseOut + "-curly-en.txt");
            writeMarkupCounts(deCurly, fnBaseOut + "-curly-de.txt");
        }

        static void writeMarkupCounts(Dictionary<string, int> counts, string fn)
        {
            List<string> lst = new List<string>();
            foreach (var x in counts) lst.Add(x.Key);
            lst.Sort((a, b) => counts[b].CompareTo(counts[a]));
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.NewLine = "\n";
                foreach (var itm in lst) sw.WriteLine(counts[itm] + "\t" + itm);
            }
        }

        static void inc(Dictionary<string, int> counter, string itm)
        {
            if (counter.ContainsKey(itm)) ++counter[itm];
            else counter[itm] = 1;
        }

        static Regex reCurly = new Regex(@"{{[^}]+}}");

        static void getMarkupRu(WiktEntry we, 
            Dictionary<string, int> poss, 
            Dictionary<string, int> meaningCurly, 
            Dictionary<string, int> enCurly, 
            Dictionary<string, int> deCurly)
        {
            inc(poss, we.PoS);
            MatchCollection mm;
            foreach (var meaning in we.Meanings)
            {
                mm = reCurly.Matches(meaning);
                foreach (Match m in mm) inc(meaningCurly, m.Value);
            }
            foreach (var trans in we.Translations)
            {
                mm = reCurly.Matches(trans);
                var counter = trans.StartsWith("en") ? enCurly : deCurly;
                foreach (Match m in mm) inc(counter, m.Value);
            }
        }

        static void cleanupRu(string fnIn, string fnOut, string fnToLem)
        {
            int cntIn = 0, cntKept = 0, cntTrans = 0, cntSpacee = 0;
            RuEntryCleaner cleaner = new RuEntryCleaner();
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnIn))
            using (var swOut = new StreamWriter(fnOut))
            using (var swToLem = new StreamWriter(fnToLem))
            {
                swOut.NewLine = "\n";
                swToLem.NewLine = "\n";
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "" && entryLines.Count > 0)
                    {
                        var we = WiktEntry.FromLinesRu(entryLines);
                        if (we != null)
                        {
                            ++cntIn;
                            var cleanEntry = cleaner.Clean(we);
                            if (cleanEntry != null)
                            {
                                ++cntKept;
                                if (cleanEntry.Translations.Count > 0) ++cntTrans;
                                if (cleanEntry.Lemma.IndexOf(' ') != -1) cntSpacee++;
                                cleanEntry.WriteRu(swOut);
                                if (cleanEntry.Lemma.IndexOf(' ') != -1) swToLem.WriteLine(cleanEntry.Lemma);
                            }
                        }
                        entryLines.Clear();
                        continue;
                    }
                    entryLines.Add(line);
                }
            }
            Console.WriteLine(cntIn + " entries / " + cntKept + " kept / " + cntTrans + " with translations / " + cntSpacee + " multiword");
        }

        static void mergeLemsRu(string fnIn, string fnPlain, string fnLems, string fnOut)
        {
            Dictionary<string, string> plainToLem = new Dictionary<string, string>();
            string l1, l2;
            using (StreamReader srPlain = new StreamReader(fnPlain))
            using (StreamReader srLems = new StreamReader(fnLems))
            {
                while ((l1 = srPlain.ReadLine()) != null)
                {
                    l2 = srLems.ReadLine();
                    string lem = "";
                    string[] parts = l2.Split(' ');
                    foreach (string p in parts)
                    {
                        if (lem != "") lem += " ";
                        int ix1 = p.IndexOf('{');
                        if (ix1 == -1) { lem += p; continue; }
                        int ix2 = p.IndexOf('|');
                        if (ix2 == -1) ix2 = p.IndexOf('}');
                        lem += p.Substring(ix1 + 1, ix2 - ix1 - 1);
                    }
                    plainToLem[l1] = lem;
                }
            }
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnIn))
            using (var swOut = new StreamWriter(fnOut))
            {
                swOut.NewLine = "\n";
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "" && entryLines.Count > 0)
                    {
                        var we = WiktEntry.FromLinesRu(entryLines);
                        if (plainToLem.ContainsKey(we.Lemma))
                            we.Lemmatized = plainToLem[we.Lemma];
                        we.WriteRu(swOut);
                        entryLines.Clear();
                        continue;
                    }
                    entryLines.Add(line);
                }
            }
        }

        static void Main(string[] args)
        {
            //dumpToEntries("_materials/dewiktionary-20200201-pages-articles.xml", "_work/_dewiktionary-01.txt", new MDParserDe());
            //entriesToDictDe("_work/_dewiktionary-01.txt", "_materials/dewiktionary.json");

            //getHead("_materials/ruwiktionary-20200601-pages-articles.xml", "_materials/ruwiktionary-head.xml", 100000);
            //dumpToEntries("_materials/ruwiktionary-20200601-pages-articles.xml", "_materials/_ruwiktionary-01.txt", new MDParserRu());
            //getMarkupRu("_materials/_ruwiktionary-01.txt", "_materials/_ruwiktionary-02");
            //cleanupRu("_materials/_ruwiktionary-01.txt", "_materials/_ruwiktionary-03.txt", "_materials/_ruwiktionary-03-plain.txt");
            // Manual step: lemmatize multi-word heads
            // mystem.exe -c ../_materials/_ruwiktionary-03-plain.txt ../_materials/_ruwiktionary-03-lem.txt
            mergeLemsRu("_materials/_ruwiktionary-03.txt", "_materials/_ruwiktionary-03-plain.txt", "_materials/_ruwiktionary-03-lem.txt", "_materials/ruwiktionary.txt");

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
