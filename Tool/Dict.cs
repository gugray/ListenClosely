using System;
using System.Collections.Generic;
using System.IO;

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
        class Meaning : IEquatable<Meaning>, IComparable<Meaning>
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

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                Meaning objAsPart = obj as Meaning;
                if (objAsPart == null) return false;
                else return Equals(objAsPart);
            }

            // Default comparer for Part type.
            public int CompareTo(Meaning comparePart)
            {
                // A null value means that this object is greater.
                if (comparePart == null) return 1;

                bool myLangBlank = (Lang == null || Lang == "");
                bool compLangBlank = (comparePart.Lang == null || comparePart.Lang == "");

                bool myTranslBlank = (Translation == null || Translation == "");
                bool compTranslBlank = (comparePart.Translation == null || comparePart.Translation == "");

                int cmpTranslations;
                if (myTranslBlank && !compTranslBlank) cmpTranslations = -1;
                else if (compTranslBlank && !myTranslBlank) cmpTranslations = 1;
                else cmpTranslations = Translation.CompareTo(comparePart.Translation);

                // my Lang = null; move up
                if (myLangBlank && !compLangBlank) return -1;
                // other lang == null; move up
                if (compLangBlank && !myLangBlank) return 1;
                // both Lang not null
                if(!myLangBlank && !compLangBlank)
                {
                    int cmp = Lang.CompareTo(comparePart.Lang);
                    // my Lang is "de"; move up
                    if ("de" == Lang && comparePart.Lang != "de") return -1;
                    // other Lang is "de"; move down
                    if ("de" == comparePart.Lang && Lang != "de") return 1;

                    // my Lang is same as other? compare the Translations
                    if (cmp == 0)
                    {
                        return cmpTranslations;
                    }
                    // sort Langs alphabetically
                    return cmp;
                }

                // both Langs are null? compare the Translations
                return cmpTranslations;
            }

            public bool Equals(Meaning other)
            {
                if (other == null) return false;
                return CompareTo(other) != 0;
            }

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

        public static readonly string acuteAccent = char.ConvertFromUtf32(0x0301);

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
                    try
                    {
                        orWords.Add(new OpenRussianWord { DisplayHead = displayHead, Id = int.Parse(parts[0]) });
                        string alt = head.Replace("ё", "е");
                        if (alt != head) alts[alt] = head;
                    }
                    catch (Exception e)
                    {
                        // Console.WriteLine("Error by parse as int the first part of line, split by tabs: '" + line + "' ('" + parts[0] + "')");
                    }
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
                    else throw new Exception("Unexpected language code: " + parts[1] + " detected in Openrussion file translations.csv");

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

            string lang = "";
            if(line.StartsWith("["))
            {
                string [] split = line.Split("]");
                if(split.Length > 1)
                {
                    lang = split[0].Substring(1);
                    line = line.Substring(line.IndexOf("]") + 1);
                }
            }

            if (isIdiom)
            {
                // add the idiom line
                var ee = addOrGetEntry(head, displayHead);
                ee.Meanings.Add(new Meaning { Translation = line, Src = srcCustom, Lang = lang });
                return;
            }
            // add the dictionary translation line
            // not a lemma replacement - easy
            if (!line.StartsWith("<="))
            {
                var ee = addOrGetEntry(head, displayHead);
                ee.Meanings.Add(new Meaning { Translation = line, Src = srcCustom, Lang = lang });
                return;
            }

            // special handling for lemma replaceents
            // clone the meanings from other entry (e.g.: the word X - same as Y)
            var sameAs = line.Substring(2).Trim();

            List<Meaning> meanings = new List<Meaning>();
            // This will produce an additional entry: "= same_as_lemma". Let us comment out.
            //meanings.Add(new Meaning { Translation = "= " + sameAs, Src = srcCustom, Lang = "ru" });

            if (headToEntries.ContainsKey(sameAs))
            {
                foreach (var ee in headToEntries[sameAs])
                    foreach (var m in ee.Meanings)
                        meanings.Add(new Meaning { Translation = m.Translation, Src = srcCustom, Lang = m.Lang });
            }

            var entry = addOrGetEntry(head, displayHead);
            entry.Meanings.AddRange(meanings);
        }

        void removeRawHead(string rawHead)
        {
            string displayHead = rawHead.Replace("'", acuteAccent);
            string head = rawHead.Replace("'", "");

            if (!headToEntries.ContainsKey(head)) return;
            Entry e = headToEntries[head].Find(x => x.DisplayHead == displayHead || x.DisplayHead == head);
            if (e != null)
            {
                headToEntries[head].Remove(e);
                if (headToEntries[head].Count == 0)
                    headToEntries.Remove(head);
            }
        }

        public void UpdateFromCustomList(string fnCustDictPath)
        {
            if(!File.Exists(fnCustDictPath))
            {
                throw new FileNotFoundException("File not found: '" + fnCustDictPath + "'");
            }

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
        /// Provide an additional sort of translations by Lang: 
        /// - first, place the entries without a language mark; 
        /// - then, the entries with "de"
        /// - the, sorted by Lang alphabetically
        /// </summary>
        public void SortByLang()
        {
            foreach (string head in headToEntries.Keys)
            {
                List<Entry> entries4head = headToEntries[head];
                foreach (Entry entry in entries4head)
                {
                    // sort the entrries by dictionary mark
                    entry.Meanings.Sort();
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


        public void indexDisplayedHeaders()
        {
            // Extend headToEntries by add also the unique references by displayed head
            // This part of index will be used for detect the translations by accented lemma
            Dict tmp = new Dict();
            foreach (string head in headToEntries.Keys)
            {
                List<Entry> entries4head = headToEntries[head];
                foreach (Entry entry in entries4head)
                {
                    if (head.Equals(entry.DisplayHead)) continue;
                    if (headToEntries.ContainsKey(entry.DisplayHead)) continue;

                    if (!tmp.headToEntries.ContainsKey(entry.DisplayHead))
                    {
                        tmp.headToEntries[entry.DisplayHead] = new List<Entry>();
                    }

                    tmp.headToEntries[entry.DisplayHead].Add(entry);
                }
            }
            foreach (string head in tmp.headToEntries.Keys)
            {
                headToEntries.Add(head, tmp.headToEntries[head]);

                string alt = head.Replace("ё", "е");
                if (alts.ContainsKey(alt)) continue;
                alts[alt] = head;
            }

            // TODO extend also wdToMultiHeads!!!
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
        public void FillDict(Material material)
        {
            List<DictEntry> entries = material.DictEntries;
            Dictionary<string, List<int>> headToIx = new Dictionary<string, List<int>>();
            foreach (var segm in material.Segments)
            {
                foreach (var word in segm.Words)
                {
                    string wdText = word.Lemma;
                    if (!string.IsNullOrEmpty(word.AccentedLemma) && (!word.AccentedLemma.Equals(wdText) || word.AccentedLemma.Contains("ё")) )
                    {
                        wdText = word.AccentedLemma;
                    } 
                    else
                    {
                        // Russian normalization
                        wdText = wdText.Replace("ё", "е");
                    }
                    string wdLo = wdText.ToLowerInvariant();

                    // Lookup - seen before
                    if (headToIx.ContainsKey(wdText))
                        word.Entries.AddRange(headToIx[wdText]);
                    else if (headToIx.ContainsKey(wdLo))
                        word.Entries.AddRange(headToIx[wdLo]);
                    // Lookup - new
                    else annotateWord(segm, word, entries, headToIx, wdText, wdLo);
                }
            }
        }

        static void addToWord(Word word, string head, List<Entry> hits,
            List<DictEntry> entries, Dictionary<string, List<int>> headToIx)
        {
            foreach (var hit in hits)
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
                if(!headToIx.ContainsKey(head))
                {
                    headToIx[head] = new List<int>();
                }
                headToIx[head].Add(ix);
                entries.Add(de);
                word.Entries.Add(ix);
            }
        }

        void annotateWord(Segment segm, Word word, List<DictEntry> entries, Dictionary<string, List<int>> headToIx,
            string wdText, string wdLo)
        {
            // Text
            if (headToEntries.ContainsKey(wdText))
                addToWord(word, wdText, headToEntries[wdText], entries, headToIx);
            // Text as alt
            else if (alts.ContainsKey(wdText) && headToEntries.ContainsKey(alts[wdText]))
                addToWord(word, alts[wdText], headToEntries[alts[wdText]], entries, headToIx);
            // Lower-case
            else if (headToEntries.ContainsKey(wdLo))
                addToWord(word, wdLo, headToEntries[wdLo], entries, headToIx);
            // Lower-case as alt
            else if (alts.ContainsKey(wdLo) && headToEntries.ContainsKey(alts[wdLo]))
                addToWord(word, alts[wdLo], headToEntries[alts[wdLo]], entries, headToIx);

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
                addToWord(word, mwHead, headToEntries[mwHead], entries, headToIx);
            }
        }
    }
}
