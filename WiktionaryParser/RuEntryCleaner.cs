using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WiktionaryParser
{

    class RuEntryCleaner
    {
        // {{abbrev|lang=ru|роль=существительного}}
        // {{сущ ru m ina |основа=|основа1=|слоги={{по-слогам|}}}}
        // {{сущ-ru|ка́ппа|ж 1a|слоги={{по-слогам|ка́п|па}}}}
        readonly Regex rePoS = new Regex(@"{{([^ \-\|]+)");

        // {{по-слогам|си|би́р|ка}}
        readonly Regex reSylls = new Regex(@"по[\- ]слогам\|([^}]+)}}");

        static Regex reCurly = new Regex(@"{{[^}]+}}");

        // [[пациент]]
        static Regex reAngle1 = new Regex(@"\[\[([^\|\]]+)\]\]");
        static Regex reAngle1B = new Regex(@"\[\[([^\|\]]*)\]\]");
        // [[необычный|необычное]]
        static Regex reAngle2 = new Regex(@"\[\[[^\|]+\|([^\]]+)\]\]");

        // {{тж.}}
        static Regex reTzh = new Regex(@"{{тж.}}");

        // {{=|химическая завивка|завивка, закрепляемая нанесением на волосы специальных реактивов}} 
        static Regex reEqual1 = new Regex(@"{{=\|([^\|}]+)\|([^}]+)}}");
        // {{=|красивый}}
        static Regex reEqual2 = new Regex(@"{{=\|([^}]+)}}");

        // {{ботан.|ru}}
        static Regex reLabel1 = new Regex(@"{{([^\|\}]+)\|ru}}");

        // {{t|en|Volk}}
        static Regex reTrans1 = new Regex(@"{{t\|[^\|}]+\|([^\}]+)}}");
        static Regex reTrans2 = new Regex(@"{{trad\|[^\|}]+\|([^\}]+)}}");

        // <barf> </barf>
        static Regex reHtml = new Regex(@"<[^>]+>");

        public WiktEntry Clean(WiktEntry entry)
        {
            WiktEntry res = new WiktEntry();
            res.Lemma = entry.Lemma;
            res.PoS = "";
            Match m = rePoS.Match(entry.PoS);
            if (m.Success) res.PoS = m.Groups[1].Value;
            if (!string.IsNullOrEmpty(entry.Pron)) res.Pron = entry.Pron;
            else res.Pron = getPronFromPoS(entry.PoS);
            if (res.Pron == "") res.Pron = entry.Lemma;
            res.Details = entry.PoS;

            foreach (var mean in entry.Meanings)
            {
                // Drop if nothing there that looks like content
                if (!anyMeaning(mean)) continue;
                // Clean meaning
                string mclean = cleanMeaning(mean);
                res.Meanings.Add(mclean);
            }
            foreach (var trans in entry.Translations)
            {
                string tclean = cleanTrans(trans);
                if (tclean.Length > 2) res.Translations.Add(tclean);
            }

            if (res.Meanings.Count == 0) return null;
            return res;
        }

        string cleanTrans(string trans)
        {
            // Convert [[word]] links into just word
            var mm = reAngle1B.Matches(trans);
            for (int i = mm.Count - 1; i >= 0; --i)
                trans = trans.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + trans.Substring(mm[i].Index + mm[i].Length);
            mm = reAngle2.Matches(trans);
            for (int i = mm.Count - 1; i >= 0; --i)
                trans = trans.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + trans.Substring(mm[i].Index + mm[i].Length);

            // Extract {{t|en|Volk}} {{trad|en|Volk}}
            mm = reTrans1.Matches(trans);
            for (int i = mm.Count - 1; i >= 0; --i)
                trans = trans.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + trans.Substring(mm[i].Index + mm[i].Length);
            mm = reTrans2.Matches(trans);
            for (int i = mm.Count - 1; i >= 0; --i)
                trans = trans.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + trans.Substring(mm[i].Index + mm[i].Length);

            // For now, remove all other {{markup}}
            while (true)
            {
                int ix = trans.IndexOf("{{");
                if (ix == -1) break;
                string str = trans.Substring(0, ix);
                ix += "{{".Length;
                for (int depth = 2; depth > 0 && ix < trans.Length; ++ix)
                {
                    if (trans[ix] == '{') ++depth;
                    if (trans[ix] == '}') --depth;
                }
                str += trans.Substring(ix);
                trans = str;
            }

            // Remove all HTML markup
            mm = reHtml.Matches(trans);
            for (int i = mm.Count - 1; i >= 0; --i)
                trans = trans.Substring(0, mm[i].Index) + trans.Substring(mm[i].Index + mm[i].Length);

            // Other format cleanup
            trans = trans.Replace("''", "");

            // Remove multiple spaces; trim
            while (true)
            {
                var str = trans.Replace("  ", " ");
                if (str.Length == trans.Length) break;
                trans = str;
            }
            trans = trans.Trim();

            // Done.
            return trans;
        }

        string cleanMeaning(string mean)
        {
            // Convert [[word]] links into just word
            var mm = reAngle1.Matches(mean);
            for (int i = mm.Count - 1; i >= 0; --i)
                mean = mean.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + mean.Substring(mm[i].Index + mm[i].Length);
            mm = reAngle2.Matches(mean);
            for (int i = mm.Count - 1; i >= 0; --i)
                mean = mean.Substring(0, mm[i].Index) + mm[i].Groups[1].Value + mean.Substring(mm[i].Index + mm[i].Length);
            // Remove examples
            while (true)
            {
                int ix = mean.IndexOf("{{пример");
                if (ix == -1) break;
                string str = mean.Substring(0, ix);
                ix += "{{пример".Length;
                for (int depth = 2; depth > 0 && ix < mean.Length; ++ix)
                {
                    if (mean[ix] == '{') ++depth;
                    if (mean[ix] == '}') --depth;
                }
                str += mean.Substring(ix);
                mean = str;
            }
            // De-markup fixed stuff:
            // {{тж.}}, {{итп}}, {{-}}
            mean = mean.Replace("{{тж.}}", "тж.");
            mean = mean.Replace("{{итп}}", "и т. п.");
            mean = mean.Replace("{{итд}}", " и т. д.");
            mean = mean.Replace("{{-}}", " — ");
            mean = mean.Replace("{{мн.}}", "мн. ч.");

            // De-markup equivalences
            mm = reEqual1.Matches(mean);
            for (int i = mm.Count - 1; i >= 0; --i)
                mean = mean.Substring(0, mm[i].Index) + "= " + mm[i].Groups[1].Value + ": " + mm[i].Groups[2].Value + mean.Substring(mm[i].Index + mm[i].Length);
            mm = reEqual2.Matches(mean);
            for (int i = mm.Count - 1; i >= 0; --i)
                mean = mean.Substring(0, mm[i].Index) + "= " + mm[i].Groups[1].Value + mean.Substring(mm[i].Index + mm[i].Length);

            // Remove pameta
            // {{помета
            while (true)
            {
                int ix = mean.IndexOf("{{помета");
                if (ix == -1) break;
                string str = mean.Substring(0, ix);
                ix += "{{помета".Length;
                for (int depth = 2; depth > 0; ++ix)
                {
                    if (mean[ix] == '{') ++depth;
                    if (mean[ix] == '}') --depth;
                }
                str += mean.Substring(ix);
                mean = str;
            }

            // Remove semantika
            // {{семантика
            while (true)
            {
                int ix = mean.IndexOf("{{семантика");
                if (ix == -1) break;
                string str = mean.Substring(0, ix);
                ix += "{{семантика".Length;
                for (int depth = 2; depth > 0; ++ix)
                {
                    if (mean[ix] == '{') ++depth;
                    if (mean[ix] == '}') --depth;
                }
                str += mean.Substring(ix);
                mean = str;
            }

            // De-markup Russian Wiktionary labels
            mm = reLabel1.Matches(mean);
            for (int i = mm.Count - 1; i >= 0; --i)
                mean = mean.Substring(0, mm[i].Index) + "{" + mm[i].Groups[1].Value + "}" + mean.Substring(mm[i].Index + mm[i].Length);

            // Whatever else remains, just obscure with [ and ]
            mean = mean.Replace("{{", "[");
            mean = mean.Replace("}}", "]");

            // Other format cleanup
            mean = mean.Replace("''", "");

            // Remove multiple spaces; trim
            while (true)
            {
                var str = mean.Replace("  ", " ");
                if (str.Length == mean.Length) break;
                mean = str;
            }
            mean = mean.Trim();

            // Done.
            return mean;
        }

        bool anyMeaning(string mean)
        {
            string pruned = mean.Substring(1);
            var mm = reCurly.Matches(pruned);
            for (int i = mm.Count - 1; i >= 0; --i)
            {
                if (mm[i].Value.StartsWith("{{=")) continue;
                pruned = pruned.Substring(0, mm[i].Index) + pruned.Substring(mm[i].Index + mm[i].Length);
            }
            pruned = pruned.Trim();
            return pruned != "";
        }

        string getPronFromPoS(string pos)
        {
            // Multiple words should work too!
            // {{phrase|тип=ф|роль=наречия|слово1={{по-слогам|жо́|пой}}|лемма1=жопа|слово2={{по-слогам|ку́|шать}}|лемма2=кушать|слово3={{по-слогам|мо́ж|но}}|лемма3=можно|lang=ru}}
            string res = "";
            var mm = reSylls.Matches(pos);
            foreach (Match m in mm)
            {
                if (res != "") res += " ";
                res += m.Groups[1].Value.Replace("|.", "").Replace("|", "");
            }
            return res;
        }
    }
}
