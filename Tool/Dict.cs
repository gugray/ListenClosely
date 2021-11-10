﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

// Openrussian: разоряться -> └ throw away

namespace Tool
{
    class Dict
    {
        const string srcOpenRussian = "openrussian";
        const string srcCustom = "custom";
        const string srcWiktionary = "wiktionary";

        /// <summary>
        /// One meaning of a headword in one language
        /// </summary>
        class Meaning
        {
            /// <summary>
            /// Target language. Two-letter code like "en", "de", "fr" etc.
            /// </summary>
            public string Lang;

            /// <summary>
            /// Source of this info (OpenRussian, Wiktionary, custom)
            /// </summary>
            public string Src;

            /// <summary>
            /// Translation into target language (or defintion in Russian)
            /// </summary>
            public string Translation;
        }

        /// <summary>
        /// One dictionary entry: a headword, and its meanings in different languages.
        /// </summary>
        class Entry
        {
            /// <summary>
            /// The headword to display. This includes accent on ё, and intonation mark.
            /// </summary>
            public string DisplayHead;

            /// <summary>
            /// The entry's meanings (translations in various languages).
            /// </summary>
            public List<Meaning> Meanings = new List<Meaning>();
        }

        /// <summary>
        /// Maps headwords (lemmas) to their dictionary entries.
        /// </summary>
        Dictionary<string, List<Entry>> headToEntries = new Dictionary<string, List<Entry>>();

        /// <summary>
        /// Key is a multi-word headword; value is each of the constituent words.
        /// </summary>
        Dictionary<string, List<string>> wdToMultiHeads = new Dictionary<string, List<string>>();

        /// <summary>
        /// Maps alternatives to thei canonical form.
        /// </summary>
        Dictionary<string, string> alts = new Dictionary<string, string>();

        static readonly string acuteAccent = char.ConvertFromUtf32(0x0301);

        class OpenRussianWord
        {
            public string DisplayHead;
            public int Id;
        }

        /// <summary>
        /// Parses OpenRussian from CSV source and constructs dictionary.
        /// </summary>
        public static Dict FromOpenRussian(string fnWords, string fnTrans)
        {
            //TODO: └  ┘
            Dict dict = new Dict();
            Dictionary<string, List<OpenRussianWord>> headToORWords = new Dictionary<string, List<OpenRussianWord>>();
            Dictionary<string, string> alts = new Dictionary<string, string>();
            Dictionary<int, List<string>> idToTransDe = new Dictionary<int, List<string>>();
            Dictionary<int, List<string>> idToTransEn = new Dictionary<int, List<string>>();
            string line;
            using (StreamReader sr = new StreamReader(fnWords))
            {
                sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 5) continue;
                    string head = parts[2];
                    string displayHead = parts[3].Replace("'", acuteAccent);
                    List<OpenRussianWord> orWords;
                    if (headToORWords.ContainsKey(head)) orWords = headToORWords[head];
                    else
                    {
                        orWords = new List<OpenRussianWord>();
                        headToORWords[head] = orWords;
                    }
                    orWords.Add(new OpenRussianWord { DisplayHead = displayHead, Id = int.Parse(parts[0]) });
                    string alt = head.Replace("ё", "е");
                    if (alt != head) alts[alt] = head;
                }
            }
            using (StreamReader sr = new StreamReader(fnTrans))
            {
                sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.IndexOfAny(new[] {'└', '┘'}) != -1)
                    {
                        line = line.Replace("└", "");
                        line = line.Replace("┘", "");
                    }
                    string[] parts = line.Split('\t');
                    if (parts.Length < 5) continue;
                    Dictionary<int, List<string>> idToTrans;
                    if (parts[1] == "en") idToTrans = idToTransEn;
                    else if (parts[1] == "de") idToTrans = idToTransDe;
                    else throw new Exception("Unexpected language code: " + parts[1]);
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
            foreach (var orItm in headToORWords)
            {
                string head = orItm.Key;
                foreach (var orWord in orItm.Value)
                {
                    if (idToTransDe.ContainsKey(orWord.Id))
                    {
                        var meanings = new List<Meaning>();
                        foreach (var y in idToTransDe[orWord.Id])
                            meanings.Add(new Meaning { Translation = y, Src = srcOpenRussian, Lang = "de" });
                        var entry = dict.addOrGetEntry(head, orWord.DisplayHead);
                        entry.Meanings.AddRange(meanings);
                    }
                    if (idToTransEn.ContainsKey(orWord.Id))
                    {
                        var meanings = new List<Meaning>();
                        foreach (var y in idToTransEn[orWord.Id])
                            meanings.Add(new Meaning { Translation = y, Src = srcOpenRussian, Lang = "en" });
                        var entry = dict.addOrGetEntry(head, orWord.DisplayHead);
                        entry.Meanings.AddRange(meanings);
                    }
                }
            }
            foreach (var x in alts) dict.alts[x.Key] = x.Value;
            return dict;
        }

        Entry addOrGetEntry(string head, string displayHead)
        {
            if (!headToEntries.ContainsKey(head))
            {
                var entry = new Entry { DisplayHead = displayHead };
                headToEntries[head] = new List<Entry>();
                headToEntries[head].Add(entry);
                return entry;
            }
            var entries = headToEntries[head];
            foreach (var entry in entries)
            {
                if (entry.DisplayHead == displayHead)
                    return entry;
            }
            var x = new Entry { DisplayHead = displayHead };
            entries.Add(x);
            return x;
        }

        void addCustomMeaning(string rawHead, string line, bool isIdiom)
        {
            string displayHead = rawHead.Replace("'", acuteAccent);
            string head = rawHead.Replace("'", "");

            if (isIdiom)
            {
                // add the idiom line
                var ee = addOrGetEntry(head, displayHead);
                ee.Meanings.Add(new Meaning { Translation = line, Src = srcCustom });
                return;
            }
            // add the dictionary translation line
            // not a lemma replacement - easy
            if (!line.StartsWith("<="))
            {
                var ee = addOrGetEntry(head, displayHead);
                ee.Meanings.Add(new Meaning { Translation = line, Src = srcCustom });
                return;
            }

            // special handling for lemma replaceents
            // clone the meanings from other entry (e.g.: the word X - same as Y)
            var sameAs = line.Substring(2).Trim();

            List<Meaning> meanings = new List<Meaning>();
            meanings.Add(new Meaning { Translation = "= " + sameAs, Src = srcCustom });

            if (headToEntries.ContainsKey(sameAs))
            {
                foreach (var ee in headToEntries[sameAs])
                    foreach (var m in ee.Meanings)
                        meanings.Add(new Meaning { Translation = m.Translation, Src = srcCustom });
            }

            var entry = addOrGetEntry(head, displayHead);
            entry.Meanings.AddRange(meanings);
        }

        void removeRawHead(string rawHead)
        {
            string displayHead = rawHead.Replace("'", acuteAccent);
            string head = rawHead.Replace("'", "");

            if (!headToEntries.ContainsKey(head)) return;
            Entry e = headToEntries[head].Find(x => x.DisplayHead == displayHead);
            if (e == null) return;
            headToEntries[head].Remove(e);
            if (headToEntries[head].Count == 0)
                headToEntries.Remove(head);
        }

        public void UpdateFromCustomList(string fnCustDictPath)
        {
            string line;
            using (StreamReader sr = new StreamReader(fnCustDictPath))
            {
                string head = null;
                string idiomBody = null;
                bool overwriteTranslationBlock = false;
                bool isIdiomatic = false;
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
                    if (head != null)
                    {
                        if (isIdiomatic) addCustomMeaning(idiomBody, line, true);
                        else addCustomMeaning(head, line, false);
                        continue;
                    }

                    // work for head part
                    head = line;
                    overwriteTranslationBlock = head.StartsWith("!");
                    if (overwriteTranslationBlock)
                    {
                        head = head.Substring(1);
                        removeRawHead(head);
                    }
                    string alt = head.Replace("ё", "е");
                    if (alt != head) alts[alt] = head;

                    // work for idiomas: if more a one word
                    isIdiomatic = head.StartsWith("%");
                    if (isIdiomatic)
                    {
                        head = head.Substring(1);
                        string[] parsed = head.Split('%');
                        string idiomHead = parsed[0];
                        idiomBody = idiomHead;
                        if (parsed.Length > 1)
                            idiomBody = parsed[1];

                        if (!wdToMultiHeads.ContainsKey(idiomHead))
                            wdToMultiHeads.Add(idiomHead, new List<string>());

                        wdToMultiHeads[idiomHead].Add(idiomBody);
                        var _ = addOrGetEntry(idiomBody, idiomBody);
                    }
                    else idiomBody = null;
                }
            }
        }

        /// <summary>
        /// Adds further target languages to dictionary from the pre-processed Wiktionary dump.
        /// </summary>
        /// <param name="fnDict">Pre-processed Wiktionary dump file name.</param>
        /// <param name="russian">If true, also adds Russian definitions.</param>
        /// <param name="langs">List of languages to extract.</param>
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
                    var displayHead = we.Pron == "" ? we.Lemma : we.Pron;
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
                    Entry entry = addOrGetEntry(head, displayHead);
                    // Retrieve translations
                    foreach (var trans in translations)
                    {
                        // Only care about requested languages
                        string lc = trans.Substring(0, 2);
                        int ix = trans.IndexOf('\t');
                        ix = trans.IndexOf('\t', ix + 1);
                        if (ix == -1) continue;
                        string trg = trans.Substring(ix + 1);
                        if (entry.Meanings.Find(x => x.Translation == trg) != null) continue;
                        entry.Meanings.Add(new Meaning { Translation = trg, Src = srcWiktionary, Lang = lc });
                    }

                    // Retrieve Russian glosses
                    if (russian)
                    {
                        foreach (var mean in we.Meanings)
                        {
                            if (mean.Length < 3) continue;
                            entry.Meanings.Add(new Meaning { Translation = mean.Substring(2), Src = srcWiktionary, Lang = "ru" });
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

        /// <summary>
        /// Annotates material with words from dictioanry.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="usingLemma"></param>
        public void FillDict(Material material, bool usingLemma)
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
                    else annotateWord(segm, word, entries, headToIx, usingLemma, wdText, wdLo);
                }
            }
        }

        static void addToWord(Word word, string head, bool usingLemma, List<Entry> hits,
            List<DictEntry> entries, Dictionary<string, int> headToIx)
        {
            foreach (var hit in hits)
            {
                // avoid duplicates (e.g. by second call, usingLemma=false)
                // this check is relevant for the second call of FillDict only
                bool willSkip = false;
                if (!usingLemma)
                {
                    foreach (DictEntry de in entries)
                    {
                        if (de.Head == head)
                        {
                            willSkip = true;
                            break;
                        }
                    }
                }

                if (!willSkip)
                {
                    DictEntry de = new DictEntry { Head = head, DisplayHead = hit.DisplayHead };
                    foreach (var sense in hit.Meanings)
                    {
                        var ds = new DictSense { SrcDef = sense.Translation };
                        if (!string.IsNullOrEmpty(sense.Lang))
                            ds.SrcDef = "[" + sense.Lang + "] " + ds.SrcDef;
                        de.Senses.Add(ds);
                    }
                    int ix = entries.Count;
                    headToIx[head] = ix;
                    entries.Add(de);
                    word.Entries.Add(ix);
                }
            }
        }

        void annotateWord(Segment segm, Word word, List<DictEntry> entries, Dictionary<string, int> headToIx,
            bool usingLemma, string wdText, string wdLo)
        {
            // Text
            if (headToEntries.ContainsKey(wdText))
                addToWord(word, wdText, usingLemma, headToEntries[wdText], entries, headToIx);
            // Text as alt
            else if (alts.ContainsKey(wdText) && headToEntries.ContainsKey(alts[wdText]))
                addToWord(word, alts[wdText], usingLemma, headToEntries[alts[wdText]], entries, headToIx);
            // Lower-case
            else if (headToEntries.ContainsKey(wdLo))
                addToWord(word, wdLo, usingLemma, headToEntries[wdLo], entries, headToIx);
            // Lower-case as alt
            else if (alts.ContainsKey(wdLo) && headToEntries.ContainsKey(alts[wdLo]))
                addToWord(word, alts[wdLo], usingLemma, headToEntries[alts[wdLo]], entries, headToIx);

            // Hint of a multi-word head
            string wdForMulti = null;
            if (wdToMultiHeads.ContainsKey(wdText)) wdForMulti = wdText;
            else if (alts.ContainsKey(wdText) && wdToMultiHeads.ContainsKey(alts[wdText])) wdForMulti = alts[wdText];
            else if (wdToMultiHeads.ContainsKey(wdLo)) wdForMulti = wdLo;
            else if (alts.ContainsKey(wdLo) && wdToMultiHeads.ContainsKey(alts[wdLo])) wdForMulti = alts[wdLo];

            if (wdForMulti == null)
                return;

            foreach (string mwHead in wdToMultiHeads[wdForMulti])
            {
                if (!isMWHit(segm, mwHead)) continue;
                addToWord(word, mwHead, usingLemma, headToEntries[mwHead], entries, headToIx);
            }
        }
    }
}
