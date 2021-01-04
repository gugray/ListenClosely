using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

namespace Tool
{
    class Dict
    {
        class Meaning
        {
            public string SrcDef;
            public List<string> OtherLangs = new List<string>();
        }

        class Entry
        {
            public string DisplayHead;
            public List<Meaning> Meanings = new List<Meaning>();
        }

        Dictionary<string, Entry> headToSenses = new Dictionary<string, Entry>();
        Dictionary<string, List<string>> wdToMultiHeads = new Dictionary<string, List<string>>();
        Dictionary<string, string> alts = new Dictionary<string, string>();

        public static Dict FromTSV1(string fnDict, string[] langs)
        {
            Dict dict = new Dict();
            string line;
            int currConceptId = -1;
            List<Meaning> currMeanings = null;
            Meaning currMeaning = null;
            string currHead = null;
            using (StreamReader sr = new StreamReader(fnDict))
            {
                sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    string p2 = parts[2];
                    string head = p2.Substring(0, p2.IndexOf('/'));
                    string def = p2.Substring(p2.IndexOf('/') + 1);
                    int conceptId = int.Parse(parts[1]);
                    if (conceptId != currConceptId)
                    {
                        currConceptId = conceptId;
                        if (head == currHead)
                        {
                            currMeanings.Add(currMeaning);
                            currMeaning = new Meaning { SrcDef = def };
                        }
                        else
                        {
                            if (currHead != null)
                            {
                                currMeanings.Add(currMeaning);
                                dict.headToSenses[currHead] = new Entry { DisplayHead = currHead, Meanings = currMeanings };
                            }
                            currHead = head;
                            currMeaning = new Meaning { SrcDef = def };
                            currMeanings = new List<Meaning>();
                        }
                    }
                    string lang = parts[4];
                    if (Array.IndexOf(langs, lang) != -1) currMeaning.OtherLangs.Add(parts[5]);
                }
            }
            return dict;
        }

        public static Dict FromORus(string fnWords, string fnTrans)
        {
            Dict dict = new Dict();
            Dictionary<string, int> rusToId = new Dictionary<string, int>();
            Dictionary<string, string> alts = new Dictionary<string, string>();
            Dictionary<int, List<string>> idToTrans = new Dictionary<int, List<string>>();
            string line;
            using (StreamReader sr = new StreamReader(fnWords))
            {
                sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 5) continue;
                    string head = parts[2];
                    rusToId[head] = int.Parse(parts[0]);
                    string alt = head.Replace("ё", "е");
                    if (alt != head) alts[alt] = head;
                }
            }
            using (StreamReader sr = new StreamReader(fnTrans))
            {
                sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 5) continue;
                    int id = int.Parse(parts[2]);
                    List<string> trans;
                    if (idToTrans.ContainsKey(id)) trans = idToTrans[id];
                    else
                    {
                        trans = new List<string>();
                        idToTrans[id] = trans;
                    }
                    trans.Add(parts[4]);
                }
            }
            foreach (var x in rusToId)
            {
                if (!idToTrans.ContainsKey(x.Value)) continue;
                string head = x.Key;
                var meanings = new List<Meaning>();
                foreach (var y in idToTrans[x.Value])
                    meanings.Add(new Meaning { SrcDef = y });
                dict.headToSenses[head] = new Entry { DisplayHead = head, Meanings = meanings };
            }
            foreach (var x in alts) dict.alts[x.Key] = x.Value;
            return dict;
        }

        public static Dict FromRuWiktionary(string fnDict)
        {
            Dict dict = new Dict();
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnDict))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "" && entryLines.Count > 0)
                    {
                        // Get wiktionary entry, create dictionary entry
                        var we = WiktEntry.FromLinesRu(entryLines);
                        string head = we.Lemma;
                        dict.headToSenses[head] = new Entry { DisplayHead = head };
                        if (we.Pron != "") dict.headToSenses[head].DisplayHead = we.Pron;
                        foreach (var mean in we.Meanings)
                        {
                            if (mean.Length < 3) continue;
                            dict.headToSenses[head].Meanings.Add(new Meaning { SrcDef = mean.Substring(2) });
                        }
                        foreach (var trans in we.Translations)
                        {
                            string lc = trans.Substring(0, 2);
                            int ix = trans.IndexOf('\t');
                            ix = trans.IndexOf('\t', ix + 1);
                            if (ix == -1) continue;
                            string trg = trans.Substring(ix + 1);
                            trg = "{" + lc + "} " + trg;
                            if (dict.headToSenses[head].Meanings.Find(x => x.SrcDef == trg) != null) continue;
                            dict.headToSenses[head].Meanings.Add(new Meaning { SrcDef = trg });
                        }
                        if (head.IndexOf(' ') != -1)
                        {
                            string[] wds = head.Split(' ');
                            foreach (var wd in wds)
                            {
                                if (!dict.wdToMultiHeads.ContainsKey(wd)) dict.wdToMultiHeads[wd] = new List<string>();
                                dict.wdToMultiHeads[wd].Add(head);
                            }
                        }
                        string alt = head.Replace("ё", "е");
                        if (alt != head) dict.alts[alt] = head;
                        // Continue reading
                        entryLines.Clear();
                        continue;
                    }
                    entryLines.Add(line);
                }
            }
            return dict;
        }

        bool isMWHit(Segment segm, string mwHead)
        {
            string[] expr = mwHead.Split(' ');
            int segmIx = 0, exprIx = 0;
            int lastMatchSegmIx = -2;
            bool hadNeighbors = false;
            while (segmIx < segm.Words.Count && exprIx < expr.Length)
            {
                string exprWd = expr[exprIx];
                string wdText = segm.Words[segmIx].Lemma;
                string wdLo = wdText.ToLowerInvariant();
                bool gotWord = false;
                gotWord |= exprWd == wdText;
                gotWord |= alts.ContainsKey(wdText) && exprWd == alts[wdText];
                gotWord |= exprWd == wdLo;
                gotWord |= alts.ContainsKey(wdLo) && exprWd == alts[wdLo];
                if (gotWord)
                {
                    if (segmIx == lastMatchSegmIx + 1) hadNeighbors = true;
                    lastMatchSegmIx = segmIx;
                    ++exprIx;
                }
                ++segmIx;
            }
            return exprIx == expr.Length && hadNeighbors;
        }

        public void FillDict(Material material)
        {
            List<DictEntry> entries = material.DictEntries;
            Dictionary<string, int> headToIx = new Dictionary<string, int>();
            foreach (var segm in material.Segments)
            {
                foreach (var word in segm.Words)
                {
                    //string wdText = word.Text;
                    string wdText = word.Lemma;
                    string wdLo = wdText.ToLowerInvariant();
                    // Russian normalization
                    wdText = wdText.Replace("ё", "е");
                    wdLo = wdLo.Replace("ё", "е");
                    // Lookup - seen before
                    if (headToIx.ContainsKey(wdText))
                        word.Entries.Add(headToIx[wdText]);
                    else if (headToIx.ContainsKey(wdLo))
                        word.Entries.Add(headToIx[wdLo]);
                    // Lookup - new
                    else
                    {
                        // Text
                        if (headToSenses.ContainsKey(wdText))
                        {
                            var hit = headToSenses[wdText];
                            DictEntry entry = new DictEntry { Head = wdText, DisplayHead = hit.DisplayHead };
                            foreach (var sense in hit.Meanings)
                            {
                                var ds = new DictSense { SrcDef = sense.SrcDef };
                                foreach (var ol in sense.OtherLangs)
                                {
                                    if (ds.OtherLangs != "") ds.OtherLangs += "; ";
                                    ds.OtherLangs += ol;
                                }
                                entry.Senses.Add(ds);
                            }
                            int ix = entries.Count;
                            headToIx[wdText] = ix;
                            entries.Add(entry);
                            word.Entries.Add(ix);
                        }
                        // Text as alt
                        else if (alts.ContainsKey(wdText) && headToSenses.ContainsKey(alts[wdText]))
                        {
                            var hit = headToSenses[alts[wdText]];
                            DictEntry entry = new DictEntry { Head = alts[wdText], DisplayHead = hit.DisplayHead };
                            foreach (var sense in hit.Meanings)
                            {
                                var ds = new DictSense { SrcDef = sense.SrcDef };
                                foreach (var ol in sense.OtherLangs)
                                {
                                    if (ds.OtherLangs != "") ds.OtherLangs += "; ";
                                    ds.OtherLangs += ol;
                                }
                                entry.Senses.Add(ds);
                            }
                            int ix = entries.Count;
                            headToIx[wdText] = ix;
                            entries.Add(entry);
                            word.Entries.Add(ix);
                        }
                        // Lower-case
                        else if (headToSenses.ContainsKey(wdLo))
                        {
                            var hit = headToSenses[wdLo];
                            DictEntry entry = new DictEntry { Head = wdLo, DisplayHead = hit.DisplayHead };
                            foreach (var sense in hit.Meanings)
                            {
                                var ds = new DictSense { SrcDef = sense.SrcDef };
                                foreach (var ol in sense.OtherLangs)
                                {
                                    if (ds.OtherLangs != "") ds.OtherLangs += "; ";
                                    ds.OtherLangs += ol;
                                }
                                entry.Senses.Add(ds);
                            }
                            int ix = entries.Count;
                            headToIx[wdLo] = ix;
                            entries.Add(entry);
                            word.Entries.Add(ix);
                        }
                        // Lower-case as alt
                        else if (alts.ContainsKey(wdLo) && headToSenses.ContainsKey(alts[wdLo]))
                        {
                            var hit = headToSenses[alts[wdLo]];
                            DictEntry entry = new DictEntry { Head = alts[wdLo], DisplayHead = hit.DisplayHead };
                            foreach (var sense in hit.Meanings)
                            {
                                var ds = new DictSense { SrcDef = sense.SrcDef };
                                foreach (var ol in sense.OtherLangs)
                                {
                                    if (ds.OtherLangs != "") ds.OtherLangs += "; ";
                                    ds.OtherLangs += ol;
                                }
                                entry.Senses.Add(ds);
                            }
                            int ix = entries.Count;
                            headToIx[wdLo] = ix;
                            entries.Add(entry);
                            word.Entries.Add(ix);
                        }
                        // Hint of a multi-word head
                        string wdForMulti = null;
                        if (wdToMultiHeads.ContainsKey(wdText)) wdForMulti = wdText;
                        else if (alts.ContainsKey(wdText) && wdToMultiHeads.ContainsKey(alts[wdText])) wdForMulti = alts[wdText];
                        else if (wdToMultiHeads.ContainsKey(wdLo)) wdForMulti = wdLo;
                        else if (alts.ContainsKey(wdLo) && wdToMultiHeads.ContainsKey(alts[wdLo])) wdForMulti = alts[wdLo];
                        if (wdForMulti != null)
                        {
                            foreach (string mwHead in wdToMultiHeads[wdForMulti])
                            {
                                if (isMWHit(segm, mwHead))
                                {
                                    var hit = headToSenses[mwHead];
                                    DictEntry entry = new DictEntry { Head = mwHead, DisplayHead = hit.DisplayHead };
                                    foreach (var sense in hit.Meanings)
                                    {
                                        var ds = new DictSense { SrcDef = sense.SrcDef };
                                        foreach (var ol in sense.OtherLangs)
                                        {
                                            if (ds.OtherLangs != "") ds.OtherLangs += "; ";
                                            ds.OtherLangs += ol;
                                        }
                                        entry.Senses.Add(ds);
                                    }
                                    int ix = entries.Count;
                                    entries.Add(entry);
                                    word.Entries.Add(ix);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
