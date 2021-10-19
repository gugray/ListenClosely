using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Tool
{
    class DictEntry
    {
        [JsonProperty("head")]
        public string Head;
        [JsonProperty("displayHead")]
        public string DisplayHead;
        [JsonProperty("senses")]
        public List<DictSense> Senses = new List<DictSense>();
    }

    class DictSense
    {
        [JsonProperty("srcDef")]
        public string SrcDef;
        [JsonProperty("otherLangs")]
        public string OtherLangs = "";
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Word
    {
        [JsonProperty("startSec")]
        public decimal StartSec;
        [JsonProperty("lengthSec")]
        public decimal LengthSec;
        [JsonProperty("glueLeft")]
        public bool GlueLeft = false;
        [JsonProperty("lead")]
        public string Lead = "";
        [JsonProperty("text")]
        public string Text = "";
        [JsonProperty("lemma")]
        public string Lemma = "";
        [JsonProperty("trail")]
        public string Trail = "";
        [JsonProperty("entries")]
        public List<int> Entries = new List<int>();

        [JsonIgnore]
        public string DebuggerDisplay
        {
            get
            {
                string res = Lead + " • " + Text + " • " + Trail;
                if (Entries.Count > 0)
                {
                    res += " ○ ";
                    for (int i = 0; i < Entries.Count; ++i)
                    {
                        if (i > 0) res += "; ";
                        res += Entries[i];
                    }
                }
                return res;
            }
        }
    }

    class Segment
    {
        [JsonProperty("startSec")]
        public decimal StartSec;
        [JsonProperty("lengthSec")]
        public decimal LengthSec;
        [JsonProperty("words")]
        public List<Word> Words = new List<Word>();
        [JsonProperty("paraIx")]
        public int ParaIx = -1;
        [JsonProperty("isTitleLine")]
        public bool IsTitleLine = false;
        [JsonProperty("isVerse")]
        public bool IsVerse = false;
        [JsonProperty("isEmptyLine")]
        public bool IsEmptyLine = false;
    }

    class Material
    {
        [JsonProperty("title")]
        public string Title = "";
        [JsonProperty("segments")]
        public List<Segment> Segments = new List<Segment>();
        [JsonProperty("dictEntries")]
        public List<DictEntry> DictEntries = new List<DictEntry>();

        public static Material FromMS(string fn)
        {
            Material material = new Material();
            string rawJsonStr = File.ReadAllText(fn);
            dynamic rawJson = JsonConvert.DeserializeObject(rawJsonStr);
            dynamic phrases = rawJson.recognizedPhrases;
            for (int i = 0; i < phrases.Count; ++i)
            {
                Segment segment = new Segment();
                int bestIx = -1;
                float bestConfidence = 0;
                for (int j = 0; j < phrases[i].nBest.Count; ++j)
                {
                    if (phrases[i].nBest[j].confidence > bestConfidence)
                    {
                        bestIx = j;
                        bestConfidence = phrases[i].nBest[j].confidence;
                    }
                }
                if (bestIx == -1) continue;
                dynamic words = phrases[i].nBest[bestIx].words;
                for (int j = 0; j < words.Count; ++j)
                {
                    dynamic jsonWord = words[j];
                    Word word = new Word
                    {
                        Text = jsonWord.word,
                        StartSec = jsonWord.offsetInTicks / 10000000,
                        LengthSec = jsonWord.durationInTicks / 10000000,
                    };
                    segment.Words.Add(word);
                }
                segment.StartSec = segment.Words[0].StartSec;
                segment.LengthSec = segment.Words[segment.Words.Count - 1].StartSec +
                    segment.Words[segment.Words.Count - 1].LengthSec -
                    segment.StartSec;
                material.Segments.Add(segment);
            }
            return material;
        }

        /// <summary>
        /// Parses material from SRT file
        /// </summary>
        public static Material FromSRT(string fn)
        {
            Regex re = new Regex(@"([^ ]+) --> ([^ ]+)");
            Material material = new Material();
            string line;
            using (StreamReader sr = new StreamReader(fn))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    var m = re.Match(line);
                    if (m.Success)
                    {
                        decimal startSec = Utils.ParseSec(m.Groups[1].Value);
                        decimal endSec = Utils.ParseSec(m.Groups[2].Value);
                        string text = "";
                        while (true)
                        {
                            line = sr.ReadLine();
                            if (line == "") break;
                            else
                            {
                                if (text != "") text += " ";
                                text += line;
                            }
                        }
                        Segment segm = new Segment
                        {
                            StartSec = startSec,
                            LengthSec = endSec - startSec,
                        };
                        buildAlfaWords(text, segm.Words);
                        material.Segments.Add(segm);
                    }
                }
            }
            return material;
        }

        static void buildAlfaWords(string text, List<Word> words)
        {
            Word word = null;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (word != null) words.Add(word);
                    word = null;
                    continue;
                }
                if (word == null) word = new Word();
                if (char.IsPunctuation(c))
                {
                    if (word.Text == "" && word.Trail == "") word.Lead += c;
                    else word.Trail += c;
                    continue;
                }
                // Mid-word punctuation followed by more letters/digits
                if (word.Trail != "")
                {
                    word.Text += word.Trail;
                    word.Trail = "";
                }
                word.Text += c;
            }
            if (word != null) words.Add(word);
        }

        public void AddLemmasRu(string fnStemmed)
        {
            string line;
            int ix = 0;
            using (StreamReader sr = new StreamReader(fnStemmed))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    Segment segm = Segments[ix];
                    ++ix;
                    List<Word> lemmas = new List<Word>();
                    buildAlfaWords(line, lemmas);
                    if (lemmas.Count != segm.Words.Count) throw new Exception("Count of lemmas (" + lemmas.Count + ") is different as count of words (" + segm.Words.Count + ")");
                    for (int i = 0; i < lemmas.Count; ++i)
                    {
                        if (lemmas[i].Lead != segm.Words[i].Lead)
                            throw new Exception("The current lemma lead '" + lemmas[i].Lead + "' if not the same as the current word lead '" + segm.Words[i].Lead  + "'");
                        if (lemmas[i].Text == "" && segm.Words[i].Text != "")
                            throw new Exception("The current lemma text is empty but the current word text is '" + segm.Words[i].Text + "'");
                        if (lemmas[i].Text != "" && segm.Words[i].Text == "")
                            throw new Exception("The current lemma text is '" + lemmas[i].Text + "' but the current word text is empty");
                        segm.Words[i].Lemma = lemmas[i].Text;
                    }
                }
            }
        }

        public void AddLemmasEn(string fnStemmed)
        {
            string line;
            int ix = 0;
            using (StreamReader sr = new StreamReader(fnStemmed))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    // DBG
                    //if (line.StartsWith("And then I be audition for something else"))
                    //    ix = ix;
                    string[] lems = line.Split(' ');
                    Segment segm = Segments[ix];
                    ++ix;
                    int i = 0;
                    foreach (var word in segm.Words)
                    {
                        if (word.Lead != "") ++i;
                        if (word.Text != "") { word.Lemma = lems[i]; ++i; }
                        if (word.Trail != "") ++i;
                    }
                    if (i != lems.Length)
                        throw new Exception("Count of words (" + i + ") is different as count of lemmas (" + lems.Length + ")");
                }
            }
        }

        static void sentSplit(string para, List<string> segs)
        {
            Regex re1 = new Regex(@"([\.\?\!\:…])( | *— *)(\p{Lu})");
            var matches = re1.Matches(para);
            int start = 0;
            string prev = "";
            foreach (Match m in matches)
            {
                string seg = prev + para.Substring(start, m.Index - start) + m.Groups[1].Value;
                segs.Add(seg);
                prev = "";
                if (m.Groups[2].Value.TrimStart() != "") prev += m.Groups[2].Value.TrimStart();
                prev+= m.Groups[3].Value;
                start = m.Index + m.Length;
            }
            segs.Add(prev + para.Substring(start));
        }

        public static Material FromPlainText(string abbreviation, bool segmentSents)
        {
            string line;
            var paras = new List<string>();
            var addParas = new List<int>();
            int addParaIx = -1;

            String fn = "_work/" + abbreviation + "-orig.txt";

            using (StreamReader sr = new StreamReader(fn))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Replace("<…>", "").Trim();
                    while (line.IndexOf("  ") != -1) line = line.Replace("  ", " ");
                    // Here, we detect an especial string <BR> 
                    // on the _start_ of a _not_empty_ line and only _one_ time in the line. 
                    // This will interpreted as: before this line, an _empty_ line 
                    // must be later generated (in the finalized segments file). 
                    // It is required for layout of verses.
                    // The index of found line numbers will be stored into an additional work file.
                    bool hasAddParMark = false;
                    if(line.StartsWith("<BR>") || line.StartsWith("<br>"))
                    {
                        hasAddParMark = true;
                        while (line.IndexOf("<BR>") != -1) line = line.Replace("<br>", "");
                        while (line.IndexOf("<br>") != -1) line = line.Replace("<br>", "");
                    }
                    if (line != "")
                    {
                        addParaIx++;
                        paras.Add(line.Trim());
                        if(hasAddParMark) addParas.Add(addParaIx);
                    }
                }
            }
            var sents = new List<string>();
            var paraStarts = new List<int>();
            foreach (var para in paras)
            {
                paraStarts.Add(sents.Count);
                if (segmentSents) sentSplit(para, sents);
                else sents.Add(para);
            }
            var res = new Material();
            int paraIx = -1;
            for (int i = 0; i < sents.Count; ++i)
            {
                if (paraStarts.Contains(i)) ++paraIx;
                var sent = sents[i];
                Segment seg = new Segment();
                buildAlfaWords(sent, seg.Words);
                seg.ParaIx = paraIx;
                res.Segments.Add(seg);
            }

            // the index of additional lines is not empty -> write the 'addpar' work file
            String addFn = "_work/" + abbreviation + "-addpar.txt";
            // delete file if exists
            File.Delete(addFn);
            if (addParas.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < addParas.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(addParas[i]);
                }
                File.WriteAllText(addFn, sb.ToString(), Encoding.UTF8);
            }

            return res;
        }

        public static Material LoadJson(string fn)
        {
            string matJson = File.ReadAllText(fn);
            Material mat = JsonConvert.DeserializeObject<Material>(matJson);
            return mat;
        }

        public void SaveJson(string fn)
        {
            string outJsonStr = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(fn, outJsonStr, Encoding.UTF8);
        }

        public void SavePlain(string fn)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var segm in Segments)
            {
                for (int i = 0; i < segm.Words.Count; ++i)
                {
                    if (i > 0) sb.Append(' ');
                    var word = segm.Words[i];
                    sb.Append(word.Lead);
                    sb.Append(word.Text);
                    sb.Append(word.Trail);
                }
                sb.Append('\n');
            }
            File.WriteAllText(fn, sb.ToString(), Encoding.UTF8);
        }
    }
}
