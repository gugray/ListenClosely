using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WiktionaryParser
{
    class MDParserDe
    {
        Regex reLemma = new Regex(@"== (.+) \({{Sprache|Deutsch}}\) ==");
        Regex rePoS = new Regex(@"=== {{Wortart\|([^\|]+)\|Deutsch}}(.*) ===");
        Regex reTranslation = new Regex(@"\*{{[^}]+}}: (\[.+)$");

        public List<WiktEntry> GetEntries(string pageTitle, string pageText)
        {
            List<WiktEntry> res = new List<WiktEntry>();
            string[] lines = pageText.Split('\n');
            var entry = new WiktEntry();
            bool afterMeanings = false;
            bool afterTranslations = false;
            foreach (var line in lines)
            {
                var m = reLemma.Match(line);
                if (m.Success)
                {
                    if (entry.Meanings.Count > 0)
                    {
                        res.Add(entry);
                        entry = new WiktEntry();
                    }
                    entry.Lemma = m.Groups[1].Value;
                    continue;
                }
                m = rePoS.Match(line);
                if (m.Success)
                {
                    if (entry.Meanings.Count > 0)
                    {
                        res.Add(entry);
                        entry = new WiktEntry { Lemma = entry.Lemma };
                    }
                    entry.PoS = m.Groups[1].Value;
                    entry.Details = m.Groups[2].Value;
                    continue;
                }
                if (line == "{{Bedeutungen}}") { afterMeanings = true; continue; }
                if (afterMeanings)
                {
                    if (line == "") { afterMeanings = false; continue; }
                    if (entry.PoS == null) continue;
                    entry.Meanings.Add(line);
                }
                if (line == "==== {{Übersetzungen}} ====") { afterTranslations = true; continue; }
                if (afterTranslations)
                {
                    if (line == "") { afterMeanings = false; continue; }
                    if (entry.Meanings.Count == 0) continue;
                    m = reTranslation.Match(line);
                    if (m.Success) entry.Translations.Add(m.Groups[1].Value);
                }
            }
            if (entry.Meanings.Count > 0) res.Add(entry);
            return res;
        }
    }
}
