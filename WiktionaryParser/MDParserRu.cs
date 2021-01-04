using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WiktionaryParser
{
    class MDParserRu
    {
        const string kMorf = "=== Морфологические и синтаксические свойства ===";
        const string kExpr = "=== Тип и синтаксические свойства сочетания ===";
        const string kMeaning = "==== Значение ====";
        const string kTrans = "=== Перевод ===";
        readonly Regex reTransBlock = new Regex(@"{{перев\-блок\|([^\|]+)");
        readonly Regex reTransItem = new Regex(@"\|(en|de)\=(.+)");

        public List<WiktEntry> GetEntries(string pageTitle, string pageText)
        {
            //// DBG
            //if (pageTitle == "да")
            //{
            //    int tre = 0;
            //}
            List<WiktEntry> res = new List<WiktEntry>();
            string[] lines = pageText.Split('\n');
            var entry = new WiktEntry();
            entry.Lemma = pageTitle;
            string transBlock = null;
            int state = 0;
            // 0: start; not in russian section
            // 1: russian section start observed
            // 2: morphology heading observed (within russian section)
            // 3: empty line observed after morphology heading
            // 4: meaning heaading observed (after russian morphology heading)
            // 5: empty line observer after meaning heading
            // 6: translation heading observed (after russian meaning heading)
            foreach (var x in lines)
            {
                // Resolve some remaining entities
                string line = cleanLine(x);
                // First: state changes
                // These all continue
                if (line.StartsWith("= {{-"))
                {
                    if (line.StartsWith("= {{-ru-}} =")) state = 1;
                    else
                    {
                        // New language section can close gathered entry
                        if (entry.Meanings.Count > 0)
                        {
                            res.Add(entry);
                            entry = new WiktEntry();
                            entry.Lemma = pageTitle;
                        }
                        state = 0;
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
                    if (state == 1) state = 2;
                    continue;
                }
                if (line == kMeaning)
                {
                    if (state == 3) state = 4;
                    continue;
                }
                if (line == kTrans)
                {
                    if (state == 5) { state = 6; transBlock = ""; }
                    continue;
                }
                // Eating content within state
                // Just after morphology header
                if (state == 2)
                {
                    if (line != "") entry.PoS += line;
                    else { state = 3; continue; }
                }
                // After meanings header
                if (state == 4)
                {
                    if (line.Trim() == "" || line.Trim() == "#") { state = 5; continue; }
                    if (line.StartsWith("#")) entry.Meanings.Add(line);
                    else if (entry.Meanings.Count > 0) entry.Meanings[entry.Meanings.Count - 1] += line;
                }
                // After translation header
                if (state == 6)
                {
                    // Some other heading: let's get out of translation state, back into 1
                    if (line.StartsWith("=")) { state = 1; continue; }
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
