using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace WiktionaryParser
{
    class Program
    {
        const int pagesPerDir = 1000;

        static void dumpToFiles(string fnDump, string baseDirFiles)
        {
            Console.WriteLine("Extracting pages into files...");
            int pageCount = 0;
            string mapFileName = Path.Combine(baseDirFiles, "pagemap.txt");
            if (!Directory.Exists(baseDirFiles)) Directory.CreateDirectory(baseDirFiles);
            string currSubDir = null;
            using (var dpr = new DumpPageReader(fnDump))
            using (var sw = new StreamWriter(mapFileName))
            {
                DumpPage page;
                while ((page = dpr.GetNextPage()) != null)
                {
                    if (pageCount % 500 == 0) Console.Write("\rExtracted " + pageCount + " pages");
                    string subDirNameRel = (pageCount / pagesPerDir).ToString("0000");
                    string subDirName = Path.Combine(baseDirFiles, subDirNameRel);
                    if (currSubDir != subDirName)
                    {
                        currSubDir = subDirName;
                        if (!Directory.Exists(currSubDir)) Directory.CreateDirectory(currSubDir);
                    }
                    string fileName = pageCount.ToString("0000000") + ".txt";
                    string fileNameFull = Path.Combine(currSubDir, fileName);
                    File.WriteAllText(fileNameFull, page.Text);
                    sw.WriteLine(page.Title + "\t" + pageCount + "\t" + Path.Combine(subDirNameRel, fileName));
                    ++pageCount;
                }
            }
            Console.WriteLine("\rFinished; total pages extracted: " + pageCount);
        }

        static int parsePage(string title, string fnPage, StreamWriter swOut, dynamic parser)
        {
            string text = File.ReadAllText(fnPage);
            List<WiktEntry> entries = parser.GetEntries(title, text);
            foreach (var entry in entries) entry.WriteRu(swOut);
            return entries.Count;
        }

        static void pagesToEntries(string fnMap, string fnEntries, dynamic parser)
        {
            Console.WriteLine("Parsing Wiktionary pages...");
            string pageBasePath = Path.GetDirectoryName(fnMap);
            int totalPageCount = 0;
            int pagesWithEntry = 0;
            int entryCount = 0;
            string msg;
            using (var sr = new StreamReader(fnMap))
            using (var sw = new StreamWriter(fnEntries))
            {
                sw.NewLine = "\n";
                string mapLine;
                while ((mapLine = sr.ReadLine()) != null)
                {
                    if (totalPageCount % 500 == 0)
                    {
                        msg = string.Format("\rParsed {0} pages; {1} with our language; {2} entries", totalPageCount, pagesWithEntry, entryCount);
                        Console.Write(msg);
                    }
                    var parts = mapLine.Split("\t");
                    string fnPage = Path.Combine(pageBasePath, parts[2]);
                    int nEntries = parsePage(parts[0], fnPage, sw, parser);
                    if (nEntries > 0) ++pagesWithEntry;
                    entryCount += nEntries;
                    ++totalPageCount;
                }
            }
            msg = string.Format("\rDone! Parsed {0} pages; {1} with our language; {2} entries", totalPageCount, pagesWithEntry, entryCount);
            Console.Write(msg);
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

            // dumpToFiles("_materials/ruwiktionary-source/ruwiktionary-20211101-pages-articles.xml", "_materials/ruwiktionary-pages");
            // pagesToEntries("_materials/ruwiktionary-pages/pagemap.txt", "_materials/ruwiktionary-work/01.txt", new MDParserRu());
            // getMarkupRu("_materials/ruwiktionary-work/01.txt", "_materials/ruwiktionary-work/02");
            // cleanupRu("_materials/ruwiktionary-work/01.txt", "_materials/ruwiktionary-work/03.txt", "_materials/ruwiktionary-work/03-plain.txt");
            // Manual step: lemmatize multi-word heads
            // mystem.exe -c ../_materials/ruwiktionary-work/03-plain.txt ../_materials/ruwiktionary-work/03-lem.txt
            mergeLemsRu("_materials/ruwiktionary-work/03.txt", "_materials/ruwiktionary-work/03-plain.txt", "_materials/ruwiktionary-work/03-lem.txt", "_materials/ruwiktionary.txt");
          
            // Console.WriteLine("Press Enter to exit.");
            // Console.ReadLine();

            // DBG: Test conversion of one page
            // string fnOut = "test-odin";
            // string title = "один";
            // string fnPage = "0495/0495167";
            // string fnOut = "test-ottuda";
            // string title = "оттуда";
            // string fnPage = "0138/0138883";
            // string fnOut = "test-nesti-ahineyu";
            // string title = "нести ахинею";
            // string fnPage = "0457/0457227";
            // string fnOut = "test-ahineya";
            // string title = "ахинея";
            // string fnPage = "0000/0000060";
            // using (var sw = new StreamWriter("_materials/ruwiktionary-work/" + fnOut + ".txt"))
            // {
            //     parsePage(title, "_materials/ruwiktionary-pages/" + fnPage + ".txt", sw, new MDParserRu());
            // }
            // getMarkupRu("_materials/ruwiktionary-work/" + fnOut + ".txt", "_materials/ruwiktionary-work/test-02");
            // cleanupRu("_materials/ruwiktionary-work/" + fnOut + ".txt", "_materials/ruwiktionary-work/test-03.txt", "_materials/ruwiktionary-work/test-03-plain.txt");

        }
    }
}
