using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WiktionaryParser
{
    class WiktEntryTransformerDe
    {
        // In Meanings:
        // Remove <ref>...</ref>
        // Remove ::, ::: etc
        // [[Berührung]]  [[spiegeln|spiegelt]]
        // ''[[Kürschnerei]]:'' dicht in Bezug auf Haare
        // {{K|Linguistik|Grammatik}} die [[Zahl]] oder die [[Anzahl]] beschriebener Begriffe
        // {{trans.}}, ''vorwiegend österreichisch, süddeutsch, mitteldeutsch'': etwas, häufig auf dem Boden liegendes
        // {{K|Weinbau|Gastronomie|fachsprachlich|ft=sonst [[regional]], besonders in [[Österreich]]|transitiv}} Beeren
        // {{trans.|:}} durch Krafteinwirkung einen Teil vom Ganzen trennen, teilen

        // In translations:
        // [1] {{Ü|en|earwig}}; [3] {{Ü|en|catchy tune}}, {{Ü|en|earworm}}; [4] {{Ü|en|earbuds}}
        // [1–4, 6, 7] {{Ü|cs|nabídka}} {{f}}

        Regex reMeanIx = new Regex(@"^:\[(\d+)\] +(.+)$");
        Regex reLink1 = new Regex(@"\[\[([^\|\]]+)\]\]");
        Regex reLink2 = new Regex(@"\[\[[^\|\]]+\|([^\]]+)\]\]");
        Regex reTrans = new Regex(@"{{trans[^}]*}}");
        Regex reK = new Regex(@"{{K\|([^}]*)}}");
        Regex reQS = new Regex(@"{{QS[^}]*}}");
        Regex reMeta1 = new Regex(@"{{([^}|]+)\|[^}]*}}");
        Regex reMeta2 = new Regex(@"{{([^}]+)}}");
        Regex reItal = new Regex(@"''([^']+)''");
        Regex reRef = new Regex(@"<ref.+</ref>");

        Regex reTxIx1 = new Regex(@"^\[([^\]]+)\] ");
        Regex reTxEn = new Regex(@"{{Ü\|en\|([^}]+)}}");

        public Entry Transform(WiktEntry we)
        {
            if (we.Lemma.Contains(":")) return null;
            if (we.PoS == "Konjugierte Form") return null;
            if (we.PoS == "Deklinierte Form") return null;
            Entry res = new Entry
            {
                Head = we.Lemma,
                PoS = we.PoS,
            };
            res.Head = reLink1.Replace(res.Head, m => m.Groups[1].Value);
            res.Head = reLink2.Replace(res.Head, m => m.Groups[1].Value);

            Dictionary<int, Meaning> numToMeaning = new Dictionary<int, Meaning>();
            for (int i = 0; i < we.Meanings.Count; ++i)
            {
                var mng = we.Meanings[i];
                if (mng.StartsWith("::")) continue;
                var mIx = reMeanIx.Match(mng);
                if (!mIx.Success) continue;
                int meanIx = int.Parse(mIx.Groups[1].Value);
                mng = mIx.Groups[2].Value;
                mng = reLink1.Replace(mng, m => m.Groups[1].Value);
                mng = reLink2.Replace(mng, m => m.Groups[1].Value);
                mng = reTrans.Replace(mng, "{trans.}");
                mng = reK.Replace(mng, m => { return "{" + m.Groups[1].Value.Replace("|", ", ") + "}"; });
                mng = reQS.Replace(mng, "");
                mng = reMeta1.Replace(mng, m => { return "{" + m.Groups[1].Value + "}"; });
                mng = reMeta2.Replace(mng, m => { return "{" + m.Groups[1].Value + "}"; });
                mng = reItal.Replace(mng, m => { return "<" + m.Groups[1].Value + ">"; });
                mng = reRef.Replace(mng, "");
                if (mng.Trim() == "") continue;
                Meaning meaning = new Meaning { SrcDef = mng };
                res.Meanings.Add(meaning);
                numToMeaning[meanIx] = meaning;
            }
            List<int> meaningIndexes = new List<int>();
            foreach (var tx in we.Translations)
            {
                string[] txs = tx.Split("; [");
                for (int i = 1; i < txs.Length; ++i) txs[i] = "[" + txs[i];
                foreach (var ln in txs)
                {
                    meaningIndexes.Clear();
                    // Which meanings does this translation refer to?
                    var mtx = reTxIx1.Match(ln);
                    if (!mtx.Success) continue;
                    string[] ixParts = mtx.Groups[1].Value.Split(',');
                    foreach (var ixPart in ixParts)
                    {
                        int val;
                        if (int.TryParse(ixPart, out val)) meaningIndexes.Add(val);
                        else
                        {
                            string ixPartNorm = ixPart.Replace('–', '-');
                            string[] fromToStr = ixPartNorm.Split('-');
                            int val2;
                            if (int.TryParse(fromToStr[0], out val) && int.TryParse(fromToStr[1], out val2))
                            {
                                for (int j = val; j <= val2; ++j) meaningIndexes.Add(j);
                            }
                        }
                    }
                    // No indexes parsed: forget it
                    if (meaningIndexes.Count == 0) continue;
                    // Gather all translations
                    var mEquivs = reTxEn.Matches(ln);
                    foreach (Match mEquiv in mEquivs)
                    {
                        foreach (int meaningIndex in meaningIndexes)
                        {
                            if (numToMeaning.ContainsKey(meaningIndex))
                                numToMeaning[meaningIndex].OtherLangs.Add(mEquiv.Groups[1].Value);
                        }
                    }
                }
            }
            return res;
        }
    }
}
