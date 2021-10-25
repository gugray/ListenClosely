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
            public string Src;
        }

        class Entry
        {
            public string DisplayHead;
            public List<Meaning> Meanings = new List<Meaning>();
        }

        Dictionary<string, Entry> headToSenses = new Dictionary<string, Entry>();
        Dictionary<string, List<string>> wdToMultiHeads = new Dictionary<string, List<string>>();
        Dictionary<string, string> alts = new Dictionary<string, string>();

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

        public void UpdateFromCustomList(string fnCustDictPath)
        {
            string line;
            using (StreamReader sr = new StreamReader(fnCustDictPath))
            {
                string head = null;
                string idiomBody = null;
                Boolean overwriteTranslationBlock = false;
                Boolean isIdiomatic = false;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    // skip the comment line
                    if (line.StartsWith("#")) continue;

                    // separator
                    if (line.Length == 0)
                    {
                        head = null;
                        idiomBody = null;
                        continue;
                    }

                    // dict. entries to the current head
                    if(head != null)
                    {
                        if (isIdiomatic)
                        {
                            // add the idiom line
                            headToSenses[idiomBody].Meanings.Add(new Meaning{SrcDef = line, Src="_cust" });
                        }
                        else
                        {
                            // ann the dictionary translation line
                            // especial handling for lemma replaceents
                            if (line.StartsWith("<="))
                            {
                                // clone the meanings from other entry (e.g.: the word X - same as Y)
                                line = line.Substring(2).Trim();

                                List<Meaning> meanings = new List<Meaning>();
                                meanings.Add(new Meaning { SrcDef = "= " + line, Src = "_cust" });

                                if (headToSenses.ContainsKey(line))
                                {
                                    foreach (Meaning m in headToSenses[line].Meanings)
                                    {
                                        meanings.Add(new Meaning { SrcDef = m.SrcDef, Src = "_cust" });
                                    }
                                }

                                if (!this.headToSenses.ContainsKey(head))
                                {
                                    // missing word: add a new Entry
                                    headToSenses.Add(head, new Entry { DisplayHead = head, Meanings = meanings });
                                }
                                else
                                {
                                    this.headToSenses[head].Meanings.AddRange(meanings);
                                }

                            }
                            else
                            {
                                if (!this.headToSenses.ContainsKey(head))
                                {
                                    // missing word: add a new Entry
                                    headToSenses.Add(head, new Entry { DisplayHead = head, Meanings = new List<Meaning>() });
                                }
                                headToSenses[head].Meanings.Add(new Meaning { SrcDef = line, Src = "_cust" });
                            }
                        }
                        continue;
                    }

                    // work for head part

                    head = line;
                    overwriteTranslationBlock = head.StartsWith("!");
                    if (overwriteTranslationBlock)
                    {
                        head = head.Substring(1);
                        if (this.headToSenses.ContainsKey(head))
                            this.headToSenses.Remove(head);
                    }
                    string alt = head.Replace("ё", "е");
                    if (alt != head) alts[alt] = head;

                    // work for idiomas: if more a one word
                    isIdiomatic = head.StartsWith("%");
                    if(isIdiomatic)
                    {
                        head = head.Substring(1);
                        string[] parsed = head.Split('%');
                        string idiomHead = parsed[0];
                        idiomBody = idiomHead;
                        if (parsed.Length > 1)
                        {
                            idiomBody = parsed[1];
                        }

                        if (!wdToMultiHeads.ContainsKey(idiomHead)) 
                        {
                            wdToMultiHeads.Add(idiomHead, new List<string>());
                        }
                        wdToMultiHeads[idiomHead].Add(idiomBody);
                        if(!headToSenses.ContainsKey(idiomBody))
                        {
                            headToSenses.Add(idiomBody, new Entry { DisplayHead = idiomBody, Meanings = new List<Meaning>() });
                        }
                    }
                    else
                    {
                        idiomBody = null;
                    }

                }
            }
        }

        public void UpdateFromRuWiktionary(string fnDict, bool russian, string[] langs)
        {
            string line;
            List<string> entryLines = new List<string>();
            using (var sr = new StreamReader(fnDict))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    // Empty line separates entries
                    if (line != "" || entryLines.Count == 0)
                    {
                        entryLines.Add(line);
                        continue;
                    }

                    // Get wiktionary entry, create dictionary entry
                    var we = WiktEntry.FromLinesRu(entryLines);
                    // New entry begins
                    entryLines.Clear();

                    // Got translations?
                    var translations = new List<string>();
                    foreach (var trans in we.Translations)
                    {
                        if (trans.Length < 3) continue;
                        string lc = trans.Substring(0, 2);
                        if (Array.IndexOf(langs, lc) == -1) continue;
                        translations.Add(trans);
                    }
                    if (!russian && translations.Count == 0) continue;

                    string head = we.Lemma;
                    Entry entry;
                    if (headToSenses.ContainsKey(head)) entry = headToSenses[head];
                    else
                    {
                        entry = new Entry { DisplayHead = head };
                        headToSenses[head] = entry;
                    }
                    // Retrieve translations
                    foreach (var trans in translations)
                    {
                        // Only care about requested languages
                        string lc = trans.Substring(0, 2);
                        int ix = trans.IndexOf('\t');
                        ix = trans.IndexOf('\t', ix + 1);
                        if (ix == -1) continue;
                        string trg = trans.Substring(ix + 1);
                        trg = "{" + lc + "} " + trg;
                        if (entry.Meanings.Find(x => x.SrcDef == trg) != null) continue;
                        entry.Meanings.Add(new Meaning { SrcDef = trg });
                    }

                    // Retrieve Russian glosses
                    if (russian)
                    {
                        foreach (var mean in we.Meanings)
                        {
                            if (mean.Length < 3) continue;
                            entry.Meanings.Add(new Meaning { SrcDef = mean.Substring(2) });
                        }
                    }

                    // Multi-word head: file separately
                    if (head.IndexOf(' ') != -1)
                    {
                        string[] wds = head.Split(' ');
                        foreach (var wd in wds)
                        {
                            if (!wdToMultiHeads.ContainsKey(wd)) wdToMultiHeads[wd] = new List<string>();
                            wdToMultiHeads[wd].Add(head);
                        }
                    }
                    string alt = head.Replace("ё", "е");
                    if (alt != head) alts[alt] = head;
                }
            }
        }

        public static Dict FromRuWiktionary(string fnDict, string[] langs)
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
                        // Retrieve Russian glosses
                        //foreach (var mean in we.Meanings)
                        //{
                        //    if (mean.Length < 3) continue;
                        //    dict.headToSenses[head].Meanings.Add(new Meaning { SrcDef = mean.Substring(2) });
                        //}
                        // Retrieve translations
                        foreach (var trans in we.Translations)
                        {
                            // Only care about requested languages
                            string lc = trans.Substring(0, 2);
                            if (Array.IndexOf(langs, lc) == -1) continue;

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

        /**
         * Detect collocations
         * @mwHead as collocation
         */
        bool isMWHit(Segment segm, string mwHead)
        {
            string[] expr = mwHead.Split(' ');
            int segmIx = 0, exprIx = 0;
            int lastMatchSegmIx = -2;
            bool hadNeighbors = false;
            while (segmIx < segm.Words.Count && exprIx < expr.Length)
            {
                string exprWd = expr[exprIx];
                string wdText = segm.Words[segmIx].Text;
                string wdLemma = segm.Words[segmIx].Lemma;
                string wdTextLo = wdText.ToLowerInvariant();
                string wdLemmaLo = wdLemma.ToLowerInvariant();

                bool gotWord = false;
                gotWord |= exprWd == wdText || exprWd == wdLemma;
                gotWord |= (alts.ContainsKey(wdText) && exprWd == alts[wdText]) || (alts.ContainsKey(wdLemma) && exprWd == alts[wdLemma]);
                gotWord |= exprWd == wdTextLo || exprWd == wdLemmaLo;
                gotWord |= (alts.ContainsKey(wdTextLo) && exprWd == alts[wdTextLo]) || (alts.ContainsKey(wdLemmaLo) && exprWd == alts[wdLemmaLo]);
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

        public void FillDict(Material material, Boolean usingLemma)
        {
            List<DictEntry> entries = material.DictEntries;
            Dictionary<string, int> headToIx = new Dictionary<string, int>();
            foreach (var segm in material.Segments)
            {
                foreach (var word in segm.Words)
                {
                    string wdText = usingLemma ? word.Lemma : word.Text;
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
                            var head = wdText;

                            // avoid duplicates (e.g. by second call, usingLemma=false)
                            // this check is relevant for the second call of FillDict only
                            Boolean willSkip = false;
                            if (!usingLemma)
                            {
                                foreach (DictEntry e in entries)
                                {
                                    if (e.Head == head)
                                    {
                                        willSkip = true;
                                        break;
                                    }
                                }
                            }

                            if (!willSkip)
                            {
                                DictEntry entry = new DictEntry { Head = head, DisplayHead = hit.DisplayHead };
                                foreach (var sense in hit.Meanings)
                                {
                                    var ds = new DictSense { SrcDef = sense.SrcDef };
                                    entry.Senses.Add(ds);
                                }
                                int ix = entries.Count;
                                headToIx[wdText] = ix;
                                entries.Add(entry);
                                word.Entries.Add(ix);
                            }

                        }
                        // Text as alt
                        else if (alts.ContainsKey(wdText) && headToSenses.ContainsKey(alts[wdText]))
                        {
                            var hit = headToSenses[alts[wdText]];
                            var head = alts[wdText];

                            // avoid duplicates (e.g. by second call, usingLemma=false)
                            // this check is relevant for the second call of FillDict only
                            Boolean willSkip = false;
                            if (!usingLemma)
                            {
                                foreach (DictEntry e in entries)
                                {
                                    if (e.Head == head)
                                    {
                                        willSkip = true;
                                        break;
                                    }
                                }
                            }

                            if(!willSkip)
                            {
                                DictEntry entry = new DictEntry { Head = head, DisplayHead = hit.DisplayHead };
                                foreach (var sense in hit.Meanings)
                                {
                                    var ds = new DictSense { SrcDef = sense.SrcDef };
                                    entry.Senses.Add(ds);
                                }
                                int ix = entries.Count;
                                headToIx[wdText] = ix;
                                entries.Add(entry);
                                word.Entries.Add(ix);
                            }
                        }
                        // Lower-case
                        else if (headToSenses.ContainsKey(wdLo))
                        {
                            var hit = headToSenses[wdLo];
                            var head = wdLo;

                            // avoid duplicates (e.g. by second call, usingLemma=false)
                            // this check is relevant for the second call of FillDict only
                            Boolean willSkip = false;
                            if (!usingLemma)
                            {
                                foreach (DictEntry e in entries)
                                {
                                    if (e.Head == head)
                                    {
                                        willSkip = true;
                                        break;
                                    }
                                }
                            }

                            if(!willSkip)
                            {
                                DictEntry entry = new DictEntry { Head = head, DisplayHead = hit.DisplayHead };
                                foreach (var sense in hit.Meanings)
                                {
                                    var ds = new DictSense { SrcDef = sense.SrcDef };
                                    entry.Senses.Add(ds);
                                }
                                int ix = entries.Count;
                                headToIx[wdLo] = ix;
                                entries.Add(entry);
                                word.Entries.Add(ix);
                            }
                        }
                        // Lower-case as alt
                        else if (alts.ContainsKey(wdLo) && headToSenses.ContainsKey(alts[wdLo]))
                        {
                            var hit = headToSenses[alts[wdLo]];
                            var head = alts[wdLo];

                            // avoid duplicates (e.g. by second call, usingLemma=false)
                            // this check is relevant for the second call of FillDict only
                            Boolean willSkip = false;
                            if (!usingLemma)
                            {
                                foreach (DictEntry e in entries)
                                {
                                    if (e.Head == head)
                                    {
                                        willSkip = true;
                                        break;
                                    }
                                }
                            }

                            if(!willSkip)
                            {
                                DictEntry entry = new DictEntry { Head = head, DisplayHead = hit.DisplayHead };
                                foreach (var sense in hit.Meanings)
                                {
                                    var ds = new DictSense { SrcDef = sense.SrcDef };
                                    entry.Senses.Add(ds);
                                }
                                int ix = entries.Count;
                                headToIx[wdLo] = ix;
                                entries.Add(entry);
                                word.Entries.Add(ix);
                            }
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
                                    // avoid duplicates (e.g. by second call, usingLemma=false)
                                    // this check is relevant for the second call of FillDict only
                                    Boolean willSkip = false;
                                    if (!usingLemma)
                                    {
                                        foreach (DictEntry e in entries)
                                        {
                                            if(e.Head == mwHead)
                                            {
                                                willSkip = true;
                                                break;
                                            }
                                        }
                                    }

                                    if(!willSkip)
                                    {
                                        var hit = headToSenses[mwHead];
                                        DictEntry entry = new DictEntry { Head = mwHead, DisplayHead = hit.DisplayHead };
                                        foreach (var sense in hit.Meanings)
                                        {
                                            var ds = new DictSense { SrcDef = sense.SrcDef };
                                            entry.Senses.Add(ds);
                                        }
                                        int ix = entries.Count;
                                        entries.Add(entry);
                                        word.Entries.Add(ix);
                                    }
                                }
                            }
                        } // if (wdForMulti != null)
                    } // else
                } // foreach (var word in segm.Words)
            } // foreach (var segm in material.Segments)
        }


    }
}
