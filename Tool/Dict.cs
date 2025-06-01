using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using static Grpc.Core.Metadata;

// Openrussian: разоряться -> └ throw away

namespace Tool
{
    class Dict
    {
        const string srcOpenRussian = "openrussian";
        const string srcCustom = "custom";
        const string srcWiktionary = "wiktionary";

        public static readonly string acuteAccent = char.ConvertFromUtf32(0x0301);


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
                if (!myLangBlank && !compLangBlank)
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

        class OpenRussianWord
        {
            public string DisplayHead;
            public int Id;
        }

        /// <summary>
        /// Public constructor
        /// </summary>
        public Dict()
        {
        }

        /// <summary>
        /// Maps headwords (lemmas) to their dictionary entries.
        /// </summary>
        private Dictionary<string, List<Entry>> headToEntries = new Dictionary<string, List<Entry>>();

        /// <summary>
        /// Key is a multi-word headword; value is each of the constituent words.
        /// </summary>
        private Dictionary<string, List<string>> wdToMultiHeads = new Dictionary<string, List<string>>();

        /// <summary>
        /// Maps alternatives to thei canonical form.
        /// </summary>
        private Dictionary<string, string> alts = new Dictionary<string, string>();

        private Entry addOrGetEntry(string head, string displayHead)
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

        private void addCustomMeaning(string rawHead, string line, bool isIdiom)
        {
            string displayHead = rawHead.Replace("'", acuteAccent);
            string head = rawHead.Replace("'", "");

            string lang = "";
            if (line.StartsWith("["))
            {
                string[] split = line.Split("]");
                if (split.Length > 1)
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

        private void removeRawHead(string rawHead)
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

        /// <summary>
        /// Parses OpenRussian from CSV source and constructs dictionary.
        /// <param name="fnWords">The path to OpenRussian file 'words.csv'.</param>
        /// <param name="fnTrans">The path to OpenRussian file 'translations.csv'.</param>
        /// </summary>
        public void UpdateFromOpenRussian(string fnWords, string fnTrans)
        {
            Dictionary<string, List<OpenRussianWord>> headToORWords = new Dictionary<string, List<OpenRussianWord>>();
            //Dictionary<string, string> alts = new Dictionary<string, string>();
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
                    if (line.IndexOfAny(new[] { '└', '┘' }) != -1)
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
                        var entry = addOrGetEntry(head, orWord.DisplayHead);
                        var meanings = new List<Meaning>();

                        foreach (var y in idToTransDe[orWord.Id])
                        {
                            AddMeaningToEntry(entry, new Meaning { Translation = y, Src = srcOpenRussian, Lang = "de" });
                        }
                    }

                    if (idToTransEn.ContainsKey(orWord.Id))
                    {
                        var entry = addOrGetEntry(head, orWord.DisplayHead);
                        var meanings = new List<Meaning>();
                        foreach (var y in idToTransEn[orWord.Id])
                        {
                            AddMeaningToEntry(entry, new Meaning { Translation = y, Src = srcOpenRussian, Lang = "en" });
                        }
                    }
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
                        string lang = trans.Substring(0, 2);

                        int ix = trans.IndexOf('\t');
                        ix = trans.IndexOf('\t', ix + 1);
                        if (ix == -1) continue;
                        string translation = trans.Substring(ix + 1);

                        AddMeaningToEntry(entry, new Meaning { Translation = translation, Src = srcWiktionary, Lang = lang });
                    }

                    // Retrieve Russian glosses
                    if (russian)
                    {
                        foreach (var mean in we.Meanings)
                        {
                            if (mean.Length < 3) continue;
                            AddMeaningToEntry(entry, new Meaning { Translation = mean.Substring(2), Src = srcWiktionary, Lang = "ru" });
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


        /// <summary>
        /// Adds translations from customer dictionary.
        /// </summary>
        /// <param name="fnCustDictPath">The path to the customer dictionary file.</param>
        public void UpdateFromCustomList(string fnCustDictPath)
        {
            if (!File.Exists(fnCustDictPath))
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
        private void AddMeaningToEntry(Entry entry, Meaning meaning)
        {
            // sort out excact duplicates
            // moved to PostFilterTranslationsByLang
            // if (entry.Meanings.Find(x => x.Translation.ToUpper() == meaning.Translation.ToUpper()) != null) return;

            // add translations from RuWiki for the language always (may cause duplicates)
            entry.Meanings.Add(meaning);
        }


        /// <summary>
        /// Filter out translations (for "en" an "de" langueges only) as per source.
        /// Reason: the de and en Translations are provided in both used dictionaries.
        /// If AddOnlyMissing is False, we simple collect translations for EN & DE from both sources.
        /// If AddOnlyMissing is True, we compair number of translation for each language, DE and EN, 
        /// found in one source, with the number of translations found in the second one. 
        /// Translations from the source hawing less entries will be ignored. 
        /// If the same number of teranslations found, we will compare the sum of lengths of translations.
        /// It this number is also the same, the OpenRussian wins.
        /// </summary>
        /// <param name="AddOnlyMissing">The flag for handle the translationd in "en" an "de" as per source.</param>
        public void FilterBySourceAndLang(bool AddOnlyMissing)
        {
            foreach (string head in headToEntries.Keys)
            {
                List<Entry> entries4head = headToEntries[head]; 
                foreach (Entry entry in entries4head)
                {
                    List<Meaning> DeMeanings = new List<Meaning>();
                    List<Meaning> EnMeanings = new List<Meaning>();
                    // translations in other languages than de / en
                    List<Meaning> other_meanings = entry.Meanings.Where(m => m.Lang != "de" && m.Lang != "en").ToList();


                    List<Meaning> PreferredDeMeanings = new List<Meaning>();
                    List<Meaning> PreferredEnMeanings = new List<Meaning>();
                    if (AddOnlyMissing)
                    {
                        // srcOpenRussian
                        // translations for language "de" from srcOpenRussian
                        List<Meaning> OpenRussianDeMeanings = entry.Meanings.Where(m => m.Src == srcOpenRussian && m.Lang == "de").ToList();
                        long OpenRussianDeMeaningsCount = OpenRussianDeMeanings.Count;
                        // total length of all translations in language "de" from srcOpenRussian
                        long OpenRussianDeMeaningsLength = 0;
                        foreach (Meaning m in OpenRussianDeMeanings) { OpenRussianDeMeaningsLength += m.Translation.Length; }

                        // translations for language "en" from srcOpenRussian
                        List<Meaning> OpenRussianEnMeanings = entry.Meanings.Where(m => m.Src == srcOpenRussian && m.Lang == "en").ToList();
                        long OpenRussianEnMeaningsCount = OpenRussianEnMeanings.Count;
                        // total length of all translations in language "en" from srcOpenRussian
                        long OpenRussianEnMeaningsLength = 0;
                        foreach (Meaning m in OpenRussianEnMeanings) { OpenRussianEnMeaningsLength += m.Translation.Length; }

                        // srcWiktionary
                        // translations for language "de" from srcWiktionary
                        List<Meaning> RuWikiDeMeanings = entry.Meanings.Where(m => m.Src == srcWiktionary && m.Lang == "de").ToList();
                        long RuWikiDeMeaningsCount = RuWikiDeMeanings.Count;
                        // total length of all translations in language "de" from srcWiktionary
                        long RusWikiDeMeaningsLength = 0;
                        foreach (Meaning m in RuWikiDeMeanings) { RusWikiDeMeaningsLength += m.Translation.Length; }

                        // translations for language "en" from srcWiktionary
                        List<Meaning> RuWikiEnMeanings = entry.Meanings.Where(m => m.Src == srcWiktionary && m.Lang == "en").ToList();
                        long RuWikiEnMeaningsCount = RuWikiEnMeanings.Count;
                        // total length of all translations in language "en" from srcWiktionary
                        long RusWikiEnMeaningsLength = 0;
                        foreach (Meaning m in RuWikiEnMeanings) { RusWikiEnMeaningsLength += m.Translation.Length; }

                        PreferredDeMeanings =
                            OpenRussianDeMeaningsCount < RuWikiDeMeaningsCount ? RuWikiDeMeanings :
                            OpenRussianDeMeaningsCount > RuWikiDeMeaningsCount ? OpenRussianDeMeanings :
                            OpenRussianDeMeaningsLength < RusWikiDeMeaningsLength ? RuWikiDeMeanings :
                            OpenRussianDeMeanings;
                        PreferredEnMeanings =
                            OpenRussianEnMeaningsCount < RuWikiEnMeaningsCount ? RuWikiEnMeanings :
                            OpenRussianEnMeaningsCount > RuWikiEnMeaningsCount ? OpenRussianEnMeanings :
                            OpenRussianEnMeaningsLength < RusWikiEnMeaningsLength ? RuWikiEnMeanings :
                            OpenRussianEnMeanings;
                    }
                    else
                    {
                        PreferredDeMeanings = entry.Meanings.Where(m => m.Lang == "de" ).ToList();
                        PreferredEnMeanings = entry.Meanings.Where(m => m.Lang == "en").ToList();
                    }


                    foreach (Meaning meaning in PreferredDeMeanings)
                    {
                        // sort out excact duplicates
                        if (DeMeanings.Find(x => x.Translation.ToUpper() == meaning.Translation.ToUpper()) != null) continue;
                        DeMeanings.Add(meaning);
                    }
                    foreach (Meaning meaning in PreferredEnMeanings)
                    {
                        // sort out excact duplicates
                        if (EnMeanings.Find(x => x.Translation.ToUpper() == meaning.Translation.ToUpper()) != null) continue;
                        EnMeanings.Add(meaning);
                    }

                    // Overwrite collected meanings in the entry after rebuild the list
                    entry.Meanings.Clear();
                    entry.Meanings.AddRange(DeMeanings);
                    entry.Meanings.AddRange(EnMeanings);
                    entry.Meanings.AddRange(other_meanings);

                    // custom sort entries by Lang
                    entry.Meanings.Sort();
                }
            }
        }

        /// <summary>
        /// Provide an additional sort of translations by Lang: 
        /// - first, place the entries without a language mark if any; 
        /// - then, the entries with "DE"
        /// - then, sorted by Lang alphabetically
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
        private bool isMWHit(Segment segm, string mwHead)
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
                    if (!string.IsNullOrEmpty(word.AccentedLemma) && (!word.AccentedLemma.Equals(wdText) || word.AccentedLemma.Contains("ё")))
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

        private void addToWord(Word word, string head, List<Entry> hits,
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
                if (!headToIx.ContainsKey(head))
                {
                    headToIx[head] = new List<int>();
                }
                headToIx[head].Add(ix);
                entries.Add(de);
                word.Entries.Add(ix);
            }
        }

        private void annotateWord(Segment segm, Word word, List<DictEntry> entries, Dictionary<string, List<int>> headToIx,
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
