using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WiktionaryParser
{
    class MDParserRu
    {
        static readonly string acuteAccent = char.ConvertFromUtf32(0x0301);
        static readonly string doubleGrave = char.ConvertFromUtf32(0x030f);
       
        const string kMorf = "=== Морфологические и синтаксические свойства ===";
        const string kPron = "=== Произношение ===";
        const string kExpr = "=== Тип и синтаксические свойства сочетания ===";
        const string kMeaning = "==== Значение ====";
        const string kTrans = "=== Перевод ===";
        readonly Regex rePronunciation = new Regex(@"{{transcription\-ru\|([^\|}]+)");
        readonly Regex rePronunciations2 = new Regex(@"{{transcriptions\-ru\|([^\|}]+)\|([^\|}]+)");
        readonly Regex reTransBlock = new Regex(@"{{перев\-блок\|([^\|]+)");
        readonly Regex reTransItem = new Regex(@"\|(en|de|it|es|fr)\=(.+)");

        public List<WiktEntry> GetEntries(string pageTitle, string pageText)
        {
            List<WiktEntry> res = new List<WiktEntry>();
            string[] lines = pageText.Split('\n');
            var entry = new WiktEntry();
            entry.Lemma = pageTitle;
            string transBlock = null;
            int state = 00;
            // 00: start; not in russian section
            // 10: russian section start observed
            // 20: morphology heading observed (within russian section)
            // 30: empty line observed after morphology heading
            // 35: pronunciation heading observed (after russian morphology heading)
            // 40: meaning heaading observed (after russian morphology heading)
            // 50: empty line observer after meaning heading
            // 60: translation heading observed (after russian meaning heading)
            foreach (var x in lines)
            {
                // Resolve some remaining entities
                string line = cleanLine(x);
                // First: state changes
                // These all continue
                if (line.StartsWith("= {{-"))
                {
                    if (line.StartsWith("= {{-ru-}} =")) state = 10;
                    else
                    {
                        // New language section can close gathered entry
                        if (entry.Meanings.Count > 0)
                        {
                            res.Add(entry);
                            entry = new WiktEntry();
                            entry.Lemma = pageTitle;
                        }
                        state = 00;
                    }
                    continue;
                }
                if (line == kMorf || line == kExpr)
                {
                    // New morphology heading can close gathered entry
                    if (entry.Meanings.Count > 0)
                    {
                        res.Add(entry);
                        entry = new WiktEntry();
                        entry.Lemma = pageTitle;
                    }
                    if (state == 10) state = 20;
                    continue;
                }
                if (line == kPron)
                {
                    if (state >= 30) state = 35;
                    continue;
                }
                if (line == kMeaning)
                {
                    if (state >= 30) state = 40;
                    continue;
                }
                if (line == kTrans)
                {
                    if (state == 50) { state = 60; transBlock = ""; }
                    continue;
                }
                // Eating content within state
                // Just after morphology header
                if (state == 20)
                {
                    if (line != "") entry.PoS += line;
                    else { state = 30; continue; }
                }
                // After pronunciation header
                if (state == 35)
                {
                    extractPron(line, entry);
                }
                // After meanings header
                if (state == 40)
                {
                    if (line.Trim() == "" || line.Trim() == "#") { state = 50; continue; }
                    if (line.StartsWith("#")) entry.Meanings.Add(line);
                    else if (entry.Meanings.Count > 0) entry.Meanings[entry.Meanings.Count - 1] += line;
                }
                // After translation header
                if (state == 60)
                {
                    // Some other heading: let's get out of translation state, back into 10
                    if (line.StartsWith("=")) { state = 10; continue; }
                    // Translation-block
                    Match m = reTransBlock.Match(line);
                    if (m.Success) { transBlock = m.Groups[1].Value; continue; }
                    // Specific translation
                    m = reTransItem.Match(line);
                    if (m.Success) entry.Translations.Add(m.Groups[1].Value + "\t" + transBlock + "\t" + m.Groups[2].Value);
                }
            }
            if (entry.Meanings.Count > 0) res.Add(entry);
            return res;
        }

        void extractPron(string line, WiktEntry entry)
        {
            Match m = rePronunciation.Match(line);
            if (m.Success)
            {
                string pron = m.Groups[1].Value.Replace(doubleGrave, "");
                string prunedPron = pron.Replace(acuteAccent, "");
                if (entry.Lemma.ToLower() == prunedPron) entry.Pron = pron;
                return;
            }
            m = rePronunciations2.Match(line);
            if (m.Success)
            {
                string pron1 = m.Groups[1].Value.Replace(doubleGrave, "");
                string prunedPron1 = pron1.Replace(acuteAccent, "");
                string pron2 = m.Groups[2].Value.Replace(doubleGrave, "");
                string prunedPron2 = pron2.Replace(acuteAccent, "");
                if (prunedPron1.ToLower() == entry.Lemma.ToLower()) entry.Pron = pron1;
                else if (prunedPron2.ToLower() == entry.Lemma.ToLower()) entry.Pron = pron2;
            }
        }

        readonly Regex reComment = new Regex(@"<!\-\-[^>]+\-\->");

        string cleanLine(string line)
        {
            line = line.Replace("&nbsp;", " ");
            line = line.Replace("&#160;", " ");
            MatchCollection mm = reComment.Matches(line);
            for (int i = mm.Count - 1; i >= 0; --i)
            {
                Match m = mm[i];
                line = line.Substring(0, m.Index) + line.Substring(m.Index + m.Length);
            }
            return line;
        }
    }
}
