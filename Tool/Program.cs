using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace Tool
{
    class Program
    { 
        static void enTokenize(Material mat)
        {
            var re = new Regex("(.+)('s|'d|'ve|'m|'re|n't)$");
            foreach (var segm in mat.Segments)
            {
                List<Word> words = new List<Word>(segm.Words.Count);
                foreach (var wd in segm.Words)
                {
                    var m = re.Match(wd.Text);
                    if (m.Success)
                    {
                        words.Add(new Word
                        {
                            Lead = wd.Lead,
                            Text = m.Groups[1].Value,
                        });
                        words.Add(new Word
                        {
                            GlueLeft = true,
                            Text = m.Groups[2].Value,
                            Trail = wd.Trail,
                        });
                    }
                    else words.Add(wd);
                }
                segm.Words = words;
            }
        }

        static void getPlainTok(Material mat, string fn)
        {
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.NewLine = "\n";
                foreach (var segm in mat.Segments)
                {
                    string line = "";
                    foreach (var wd in segm.Words)
                    {
                        if (wd.Lead != "") { if (line != "") line += ' '; line += wd.Lead; }
                        if (wd.Text != "") { if (line != "") line += ' '; line += wd.Text; }
                        if (wd.Trail != "") { if (line != "") line += ' '; line += wd.Trail; }
                    }
                    sw.WriteLine(line);
                }
            }
        }

        static void doRus(string ep)
        {
            CsvToSrt.ConvertFile("_work/" + ep + ".csv", "_work/" + ep + ".srt");
            Material mat = Material.FromSRT("_work/" + ep + ".srt");
            // Plain text is already done by hand
            mat.AddLemmasRu("_work/" + ep + "-lem.txt");
            Dict dict = Dict.FromORus("_materials/openrussian/words.csv", "_materials/openrussian/translations.csv");
            dict.FillDict(mat);
            mat.SaveJson("_work/" + ep + "-segs.json");
            mat.SaveJson("Player/public/media/" + ep + "-segs.json");
        }

        static void doEn(string ep, string[] langs)
        {
            //CsvToSrt.ConvertFile("_work/" + ep + ".csv", "_work/" + ep + ".srt");
            //Material mat = Material.FromSRT("_work/" + ep + ".srt");
            //enTokenize(mat); // This splits didn't and I'm and girl's etc.
            //getPlainTok(mat, "_work/" + ep + "-tok.txt");
            //mat.SaveJson("_work/" + ep + "-pre-lem.json");
            //// Manual step here: Lemmatize via Python
            //// >> ep-lem.txt

            var xmat = Material.LoadJson("_work/" + ep + "-pre-lem.json");
            xmat.AddLemmasEn("_work/" + ep + "-lem.txt");
            Dict dict = Dict.FromTSV1("_materials/translations.tsv", langs);
            dict.FillDict(xmat);
            xmat.SaveJson("_work/" + ep + "-segs.json");
            xmat.SaveJson("Player/public/media/" + ep + "-segs.json");
        }

        static void getDeSurfs(Material mat, string fn)
        {
            var wdSetLo = new HashSet<string>();
            foreach (var segm in mat.Segments)
            {
                foreach (var wd in segm.Words)
                {
                    string wdLo = wd.Text.ToLower();
                    if (wdLo == "") continue;
                    wdSetLo.Add(wdLo);
                }
            }
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.NewLine = "\n";
                foreach (var x in wdSetLo) sw.WriteLine(x);
            }
        }

        static void addGerVerbLemmas(Material mat, string fnSurfs, string fnVerbLems)
        {
            string ln1, ln2;
            var surfToLem = new Dictionary<string, string> ();
            using (var srSurfs =new StreamReader(fnSurfs))
            using (var srVerbLems = new StreamReader(fnVerbLems))
            {
                while ((ln1 = srSurfs.ReadLine()) != null)
                {
                    ln2 = srVerbLems.ReadLine();
                    surfToLem[ln1] = ln2;
                }
            }
            foreach (var segm in mat.Segments)
            {
                foreach (var wd in segm.Words)
                {
                    if (!surfToLem.ContainsKey(wd.Text.ToLower())) continue;
                    wd.Lemma = surfToLem[wd.Text.ToLower()];
                }
            }
        }

        static void shiftSegments(Material mat, decimal ofs)
        {
            foreach (var segm in mat.Segments)
            {
                if (segm.StartSec > 0) segm.StartSec += ofs;
            }
        }

        static void doOrigAlignRus(string ep, decimal shift, string title)
        {
            string transJson = "_work/" + ep + "-goog.json";
            // Transcribe text with Google, if file does not exist yet
            if (!File.Exists(transJson))
            {
                GoogleTranscriber gt = new GoogleTranscriber("ServiceAccountKey.json");
                var trans = gt.Transcribe("_audio/" + ep + ".flac", "ru");
                trans.Title = title;
                trans.SaveJson(transJson);
            }

            // Re-load - just to make it easier to uncomment part above independently
            var mTrans = Material.LoadJson(transJson);
            mTrans.Title = title;

            // Read original text, and segment paragraphs
            var mOrig = Material.FromPlainText("_work/" + ep + "-orig.txt", true);
            mOrig.Title = title;
            // Save as plain text, for lemmatization
            mOrig.SavePlain("_work/" + ep + "-plain.txt");

            // Align, and infuse timestamps
            TimeFuser fs = new TimeFuser(mTrans, mOrig);
            fs.Fuse();

            // Stop here until -lem file is done by rulem.py
            //return;

            // Shift all segment timestamps... Don't ask why
            shiftSegments(mOrig, shift);

            // MANUAL STEP HERE: Run rulem.py on ep-plain.txt
            mOrig.AddLemmasRu("_work/" + ep + "-lem.txt");
            Dict dict = Dict.FromORus("_materials/openrussian/words.csv", "_materials/openrussian/translations.csv");
            if (File.Exists("_materials/ru-custom.txt")) 
            dict.FillDict(mOrig);
            //Dict dict2 = Dict.FromRuWiktionary("_materials/ruwiktionary.txt");
            //dict2.FillDict(mOrig);
            mOrig.SaveJson("_work/" + ep + "-segs.json");
            mOrig.SaveJson("ProsePlayer/public/media/" + ep + "-segs.json");
        }

        static void Main(string[] args)
        {
            // FLAC onversion with ffmpeg for Google:
            // ffmpeg -i RTO.mp3 -af aformat=s16:16000 -ac 1 RTO.flac

            // The English process
            //doEn("FAJW", new string[] { "Hungarian", "German" });

            // The Russian process
            //doRus("RTO");

            // Audio B: transcribe online, then infuse timestamp data into original text via alignment
            doOrigAlignRus("RTO", (decimal)0.00, "Лев Толстой: Война и мир");
            //doOrigAlignRus("RCS", (decimal)0.35, "Чехов: Студент");
            //doOrigAlignRus("RCANS", 0, "Чехов: Анна на шее");
            //doOrigAlignRus("RTPB", (decimal)-0.08, "Лев Толстой: После бала");
            //doOrigAlignRus("RCG", (decimal)-0.12, "Антон Чехов: Гриша");
            //doOrigAlignRus("RTD", (decimal)-0.12, "Лев Толстой: Детство");
            //doOrigAlignRus("SAMPLE", (decimal)0.35, "Чехов: Анна на шее");
        }
    }
}
