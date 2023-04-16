using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace WiktionaryParser
{
    class Program
    {
        // The main work directory
        private const string MATERIALS_DIR_PATH = "_materials";
        // Directory for save the extracted pages data
        private const string DUMP_PAGES_EXTRACT_DIR = "ruwiktionary-pages";
        // directory for save the temporary work data
        private const string DUMP_TMP_WORK_DIR = "ruwiktionary-work";

        // Pagemap file name
        private const string PAGEMAP_FILE = "pagemap.txt";
        // The file name for save the entries
        private const string ENTRIES_FILE = "01.txt";
        // This file part is used for creation the woirk files:
        // "02-curly-poss.txt", "02-curly-meanings.txt", "02-curly-en.txt", "02-curly-de.txt");
        private const string ENTRIES_OUT_FILES_BASENAME = "02";
        // The cleanup work file
        private const string CLEANUP_FILE = "03.txt";
        // The work file used for lemmatization
        private const string CLEANUP_PLAIN_FILE = "03-plain.txt";
        // The lemmatization output work file containing lemmas
        private const string CLEANUP_LEM_FILE = "03-lem.txt";
        // The final export filename
        private const string RUWIKTIONARY_EXPORT_FILE = "ruwiktionary.txt";

        private const int PAGES_PER_DIR = 1000;
        private const string START_PAGE_PATTERN = "<page>";

        private static void dumpToFiles(string fnDump, string workDir)
        {
            string baseDirFiles = Path.Combine(workDir, DUMP_PAGES_EXTRACT_DIR);
            string line;
            int pagesCountTotal = 0;
            using (StreamReader sr = new StreamReader(fnDump))
            {
                // only single-line property entries are supported!
                while ((line = sr.ReadLine()) != null)
                {
                    if(line.ToLower().Contains(START_PAGE_PATTERN))
                    {
                        pagesCountTotal++;
                    }
                }
            }

            Console.WriteLine("Extracting " + pagesCountTotal + " pages into files...");
            int pageCount = 0;
            string mapFileName = Path.Combine(baseDirFiles, PAGEMAP_FILE);
            if (!Directory.Exists(baseDirFiles)) Directory.CreateDirectory(baseDirFiles);
            string currSubDir = null;
            using (var dpr = new DumpPageReader(fnDump))
            using (var sw = new StreamWriter(mapFileName))
            {
                DumpPage page;
                while ((page = dpr.GetNextPage()) != null)
                {
                    if (pageCount > 0 && pageCount % 500 == 0)
                    {
                        double percentage = Math.Round(((double)pageCount / (double)pagesCountTotal * 100D), 2);
                        Console.Write("\rExtracted " + pageCount + " pages of " + pagesCountTotal + " (" + percentage + " % ready)");
                    }
                    string subDirNameRel = (pageCount / PAGES_PER_DIR).ToString("0000");
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

        private static int parsePage(string title, string fnPage, StreamWriter swOut, dynamic parser)
        {
            string text = File.ReadAllText(fnPage);
            List<WiktEntry> entries = parser.GetEntries(title, text);
            foreach (var entry in entries) entry.WriteRu(swOut);
            return entries.Count;
        }


        private static void pagesToEntries(string workDir, dynamic parser)
        {
            string baseDirFiles = Path.Combine(workDir, DUMP_PAGES_EXTRACT_DIR);
            string fnMap = Path.Combine(baseDirFiles, PAGEMAP_FILE);

            string tmpWorkDir = Path.Combine(workDir, DUMP_TMP_WORK_DIR);
            string fnEntries = Path.Combine(tmpWorkDir, ENTRIES_FILE); 

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

        private static void entriesToDictDe(string fnEntries, string fnJson)
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

        private static void getMarkupRu(string workDir)
        {
            string entriesDir = Path.Combine(workDir, DUMP_TMP_WORK_DIR);
            string fnEntries = Path.Combine(entriesDir, ENTRIES_FILE);
            string fnBaseOut = Path.Combine(entriesDir, ENTRIES_OUT_FILES_BASENAME);

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

        private static void writeMarkupCounts(Dictionary<string, int> counts, string fn)
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

        private static void inc(Dictionary<string, int> counter, string itm)
        {
            if (counter.ContainsKey(itm)) ++counter[itm];
            else counter[itm] = 1;
        }

        private static void getMarkupRu(WiktEntry we, 
            Dictionary<string, int> poss, 
            Dictionary<string, int> meaningCurly, 
            Dictionary<string, int> enCurly, 
            Dictionary<string, int> deCurly)
        {
            Regex reCurly = new Regex(@"{{[^}]+}}");
            
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

        private static void cleanupRu(string workDir)
        {
            string tmpWorkDir = Path.Combine(workDir, DUMP_TMP_WORK_DIR);
            string fnIn = Path.Combine(tmpWorkDir, ENTRIES_FILE);
            string fnOut = Path.Combine(tmpWorkDir, CLEANUP_FILE);
            string fnToLem = Path.Combine(tmpWorkDir, CLEANUP_PLAIN_FILE);


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

        private static void mergeLemsRu(string workDir)
        {
            string tmpWorkDir = Path.Combine(workDir, DUMP_TMP_WORK_DIR);
            string fnIn = Path.Combine(tmpWorkDir, CLEANUP_FILE);
            string fnPlain = Path.Combine(tmpWorkDir, CLEANUP_PLAIN_FILE);
            string fnLems = Path.Combine(tmpWorkDir, CLEANUP_LEM_FILE);
            string fnOut = Path.Combine(workDir, RUWIKTIONARY_EXPORT_FILE);


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


        /**
         * Call Yandex mystem lemmatizer as system process with:
         * mystem.exe -c [workDir]/ruwiktionary-work/03-plain.txt [workDir]/_materials/ruwiktionary-work/03-lem.txt
         */
        private static void callMyStem(string mystemPath, string workDir)
        {
            string tmpWorkDir = Path.Combine(workDir, DUMP_TMP_WORK_DIR);
            string fnToLem = Path.Combine(tmpWorkDir, CLEANUP_PLAIN_FILE);
            string fnLems = Path.Combine(tmpWorkDir, CLEANUP_LEM_FILE);

            if (!Directory.Exists(tmpWorkDir)) Directory.CreateDirectory(tmpWorkDir);

            ProcessStartInfo start = new ProcessStartInfo();
            // start.EnvironmentVariables.Add("pymystem3.constants.MYSTEM_BIN", toAbsolutePath(SCRIPTS_DIR_PATH));
            start.FileName = "\"" + mystemPath + "\"";
            start.Arguments = " -c \"" + fnToLem + "\" \"" + fnLems + "\"";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            try
            {
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from program
                        string result = reader.ReadToEnd(); // Here is the result of StdOut

                        if (stderr != null && stderr.Length > 0)
                        {
                            throw new InvalidProgramException("Errors occured by run 'Mystem': " + stderr);
                        }
                    }
                }
            }
            catch (Win32Exception e)
            {
                throw new InvalidProgramException("Cannot run 'Mystem': " + e.Message, e);
            }


        }


        static void Main(string[] args)
        {
            // The main work directory
            string workDir = new FileInfo(MATERIALS_DIR_PATH).FullName;
            // Path to Ruwiktionary import file nane
            string dumpFile         = "C:/Projekte/ListenClosely/_materials/ruwiktionary-20221120-pages-articles.xml";
            // Absolute path to executable mystem.exe (part of Yandex MyStem)
            string mystemPath       = "C:/Projekte/ListenClosely/Scripts/mystem.exe";

            // IN -> 01
            dumpToFiles(dumpFile, workDir);
            // 
            pagesToEntries(workDir, new MDParserRu());
            // 01 -> 02
            getMarkupRu(workDir);
            // 02 -> 03
            cleanupRu(workDir);

            // Manual step: lemmatize multi-word heads
            // mystem.exe -c ../_materials/ruwiktionary-work/03-plain.txt ../_materials/ruwiktionary-work/03-lem.txt
            callMyStem(mystemPath, workDir);

            // 03 -> OUT
            mergeLemsRu(workDir);

        }
    }
}
