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
        static void shiftSegments(Material mat, decimal ofs)
        {
            foreach (var segm in mat.Segments)
            {
                if (segm.StartSec > 0) segm.StartSec += ofs;
            }
        }


        /**
         * If the FLAC audio file was converted using the atempo filter 
         * which may be helpful for avoid the speech recognition errors on the Google side,
         * then revert the duration and the start time to the initial values
         */
        static void changeSegmentsForTempo(Material mat, double tempoCorrection)
        {
            if (tempoCorrection == 0.0)
            {
                return;
            }

            foreach (var segm in mat.Segments)
            {
                segm.StartSec = (decimal)((double)segm.StartSec * tempoCorrection);
                segm.LengthSec = (decimal)((double)segm.LengthSec * tempoCorrection);
                foreach (var wd in segm.Words)
                {
                    wd.StartSec = (decimal)((double)segm.StartSec * tempoCorrection);
                    wd.LengthSec = (decimal)((double)wd.LengthSec * tempoCorrection);
                }
            }
        }

        /**
         * This method will add an empty segment on the given position
         * 
         */
        static void shiftSegments(Material mat, int position, bool markAsTitle)
        {
            if (position == 0)
            {
                return;
            }

            // less as 2 segments - no sense to add a line
            int i = mat.Segments.Count;
            if (i < 2)
            {
                return;
            }

            // requested line position >= the end position
            if (position >= mat.Segments[i - 1].ParaIx)
            {
                return;
            }

            i = mat.Segments.Count - 1;
            for (; i > 0; i--)
            {
                if (mat.Segments[i].ParaIx == position)
                {
                    break;
                }
            }
            mat.Segments.Insert(i, new Segment());

            // clean the segment on the current position for have no data
            // zero duration
            mat.Segments[i].LengthSec = 0;
            // start point the same as end point of previous segment
            mat.Segments[i].StartSec = mat.Segments[i - 1].StartSec + mat.Segments[i - 1].LengthSec;
            // the paragraph idx 
            mat.Segments[i].ParaIx = mat.Segments[i - 1].ParaIx + 1;
            mat.Segments[i].IsEmptyLine = true;
            // list of words must contain one empty word
            mat.Segments[i].Words.Add(new Word());
            mat.Segments[i].Words[0].Text = "";
            mat.Segments[i].Words[0].Lemma = "";
            mat.Segments[i].Words[0].Lead = "";
            mat.Segments[i].Words[0].Trail = "";
            // zero duration
            mat.Segments[i].Words[0].LengthSec = 0;
            // start point the same as start point of this segment
            mat.Segments[i].Words[0].StartSec = mat.Segments[i].StartSec;

            i++;
            for (; i < mat.Segments.Count; i++)
            {
                ++mat.Segments[i].ParaIx;
            }

            // also, mark the title lines
            if (markAsTitle)
            {
                i = 0;
                for (; i < position; i++)
                {
                    mat.Segments[i].IsTitleLine = true;
                }
            }
        }

        /**
         * This method will take first N lines and mark they as a separate paragraph. 
         * To be used for mark the title lines in the verses.
         */
        static void shiftTitleSegments(Material mat, int shiftTitleLines)
        {
            shiftSegments(mat, shiftTitleLines, true);
        }

        static void markVerses(Material mat)
        {
            foreach (Segment seg in mat.Segments)
            {
                if (!seg.IsTitleLine)
                {
                    seg.IsVerse = true;
                }
            }
        }

        /**
         *  This method will search for the 'addpar' file and parfe the content a a list of numbers, 
         *  then, for each number, it will add an empoty segment
         */
        static void shiftAdditionalParas(Material mat, string abbreviation, int shiftTitleLines)
        {
            String addFn = "_work/" + abbreviation + "-addpar.txt";
            if (!File.Exists(addFn))
            {
                return;
            }
            string line = File.ReadAllText(addFn);
            if (line.Trim().Length == 0) return;

            string[] parts = line.Split(',');
            if (parts.Length == 0) return;

            List<int> addPars = new List<int>();
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val))
                    addPars.Add(int.Parse(parts[i]));
            }
            if (addPars.Count == 0) return;

            // add empty segments
            for (int i = 0; i < addPars.Count; i++)
            {
                int addPar = addPars[i];
                if (shiftTitleLines > 0) addPar++;
                addPar += i;
                shiftSegments(mat, addPar, false);
            }
        }

        /**
         * useWords             if true, search translation not only for the lemma but also for the original word
         * abbreviation         abbreviated name of the work data
         * shift                the value to shift the segments timestamps
         * tempoCorrection      the value for tempo correction (0 if not required)
         * customDictFileName   the file name  of custom dictionary (nullable)
         * title                the work title, which will be displayed on the page
         * veses                the flag for mark main text as veses lines
         * shiftTitleLines      the first X lines which will be marked as title lines of the text
         * breakWork            to be set true until the -lem file is still not done by rulem.py
         * useMS                parse transcription by MS; otherwise, get Google transcription via API
         */
        static void doOrigAlignRus(bool useWords, string abbreviation, decimal shift, double tempoCorrection, string customDictFileName,
            string title, int shiftTitleLines, bool verses, bool breakWork, bool useMS)
        {
            string transJson;
            Material trans = null;
            // Using Google?
            if (!useMS)
            {
                transJson = "_work/" + abbreviation + "-goog.json";
                // If transcription is missing, get it now
                if (!File.Exists(transJson))
                {
                    // TO-DO
                    // // Transcribe text with Google engine
                    // GoogleTranscriber gt = new GoogleTranscriber("ServiceAccountKey.json");
                    // trans = gt.Transcribe("_audio/" + abbreviation + ".flac", "ru");
                    // // Set title, serialize
                    // trans.Title = title;
                    // trans.SaveJson(transJson);
                }
            }
            // Using MS?
            else
            {
                transJson = "_work/" + abbreviation + "-ms.json";
                // -conv-ms.json is the direct output of the MS service
                // It is nout our own Material class serialized
                trans = Material.FromMS("_work/" + abbreviation + "-conv-ms.json");
            }
            // We have a new transcription: save it
            if (trans != null)
            {
                // Set title, serialize
                trans.Title = title;
                trans.SaveJson(transJson);
            }

            // Re-load - just to make it easier to uncomment part above independently
            var mTrans = Material.LoadJson(transJson);
            mTrans.Title = title;

            // Read original text, and segment paragraphs
            var mOrig = Material.FromPlainText(abbreviation, true);
            mOrig.Title = title;
            // Save as plain text, for lemmatization
            mOrig.SavePlain("_work/" + abbreviation + "-plain.txt");

            // Align, and infuse timestamps
            TimeFuser fs = new TimeFuser(mTrans, mOrig);
            fs.Fuse();

            // Stop here until -lem file is done by rulem.py
            if (breakWork)
            {
                return;
            }

            // MANUAL STEP HERE: Run rulem.py on ep-plain.txt

            // Shift all segment timestamps... Don't ask why
            shiftSegments(mOrig, shift);

            mOrig.AddLemmasRu("_work/" + abbreviation + "-lem.txt");
            Dict dict = Dict.FromOpenRussian("_materials/openrussian/words.csv", "_materials/openrussian/translations.csv");
            dict.UpdateFromRuWiktionary("_materials/ruwiktionary.txt", false, new string[] { "it", "es", "fr" });
            if (customDictFileName != null && File.Exists("_materials/" + customDictFileName))
            {
                // Extend/override the dictionary by additional customized dictionary
                dict.UpdateFromCustomList("_materials/" + customDictFileName);
            }

            // compose the lemmas-based translations
            dict.FillDict(mOrig, true);
            // compose the text-based translations - only if requested especially
            if (useWords)
            {
                dict.FillDict(mOrig, false);
            }

            // Workaround for mark the title lines if required
            shiftTitleSegments(mOrig, shiftTitleLines);

            // Workaround for mark the empty lines between the strophes of verses if required
            shiftAdditionalParas(mOrig, abbreviation, shiftTitleLines);

            if (verses)
            {
                markVerses(mOrig);
            }

            changeSegmentsForTempo(mOrig, tempoCorrection);

            mOrig.SaveJson("_work/" + abbreviation + "-segs.json");
            mOrig.SaveJson("ProsePlayer/public/media/" + abbreviation + "-segs.json");

            var rg = new ReviewGenerator();
            rg.Print(mOrig, "_work/" + abbreviation + "-annot.html");
        }

        static void Main(string[] args)
        {
            string customDictFileName = "ru-custom.txt";

            bool breakWork = false;  // for 1st start, set true; for 2nd start, set false

            bool useWords = false;   // set normally false, else, the forms of words will be used additionally to lemmas dor collect the translations
            bool useMs = true;       // set true for use MS Speech2Text API, else false for use the Google engine
            double shift = 0.0;
            double tempoCorrection = 0.0;

            string abbreviation;
            string title;
            int shiftTitleLines; // the count of title lines; an additional empty paragraph will be add after
            bool verses = false;

            abbreviation = "MLE_FAT_1";
            title = "Михаил Лермонтов - Фаталист";
            shiftTitleLines = 0;
            tempoCorrection = 0.0;
            verses = false;
            doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            // abbreviation = "APT_BKR_1";
            // title = "А. С. Пушкин. Барышня-крестьянка (1). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            // abbreviation = "APT_BKR_2";
            // title = "А. С. Пушкин. Барышня-крестьянка (2). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);




            //abbreviation = "MLE_GOV";
            //title = "М. Ю. Лермонтов. Из Гете. Читает Даниил Казбеков";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            //abbreviation = "MLE_PNT";
            //title = "М. Ю. Лермонтов. Посреди небесных тел... Читает Михаил Казбеков";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            //abbreviation = "MLE_VOD";
            //title = "М. Ю. Лермонтов. Выхожу один я на дорогу... Читает Даниил Казбеков";
            //shiftTitleLines = 1;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_FAT_1";
            // title = "М. Ю. Лермонтов. Фаталист. Из романа «Герой нашего времени» (1). Читает Евгений Шибаров";
            // shiftTitleLines = 3;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_FAT_2";
            // title = "М. Ю. Лермонтов. Фаталист. Из романа «Герой нашего времени» (2). Читает Евгений Шибаров";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_FAT_3";
            // title = "М. Ю. Лермонтов. Фаталист. Из романа «Герой нашего времени» (3). Читает Евгений Шибаров";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_FAT_4";
            // title = "М. Ю. Лермонтов. Фаталист. Из романа «Герой нашего времени» (4). Читает Евгений Шибаров";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_FAT_5";
            // title = "М. Ю. Лермонтов. Фаталист. Из романа «Герой нашего времени» (5). Читает Евгений Шибаров";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_ANS_1";
            // title = "А. П. Чехов. Анна на шее (1). Читает Анна Шибарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_ANS_2";
            // title = "А. П. Чехов. Анна на шее (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "ATCH_ANS_3";
            // title = "А. П. Чехов. Анна на шее (3). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "ATCH_ANS_4";
            // title = "А. П. Чехов. Анна на шее (4). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //  
            // abbreviation = "ATCH_GFR";
            // title = "А. П. Чехов. Глупый француз. Читает Владимир Иванчин";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "LTL_RTD";
            // title = "Лев Толстой. Детство. Читает Анна Шибарова";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "LTL_VIM";
            // title = "Лев Толстой: Война и мир. Читает Анна Шибарова";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "ATCH_STU_1";
            // title = "А. П. Чехов. Студент (1). Читает Анна Шибарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_STU_2";
            // title = "А. П. Чехов. Студент (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "APT_MET_1";
            // title = "А. С. Пушкин. Метель (1). Читает Анна Шибарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_2";
            // title = "А. С. Пушкин. Метель (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_3";
            // title = "А. С. Пушкин. Метель (3). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_4";
            // title = "А. С. Пушкин. Метель (4). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_5";
            // title = "А. С. Пушкин. Метель (5). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "APT_BKR_1";
            // title = "А. С. Пушкин. Барышня-крестьянка (1). Читает Влада Гехтман";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_2";
            // title = "А. С. Пушкин. Барышня-крестьянка (2). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_3";
            // title = "А. С. Пушкин. Барышня-крестьянка (3). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_4";
            // title = "А. С. Пушкин. Барышня-крестьянка (4). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_5";
            // title = "А. С. Пушкин. Барышня-крестьянка (5). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            //abbreviation = "MLE_PAR";
            //title = "Михаил Лермонтов. Парус. Читает Вениамин Ицкович";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            //abbreviation = "APT_DMJ";
            //title = "А. С. Пушкин. В альбом Павлу Вяземскому. Читает Михаил Казбеков";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            //abbreviation = "APT_EZH";
            //title = "А. С. Пушкин. Если жизнь тебя обманет. Читает Михаил Казбеков";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

            //abbreviation = "APT_SSN";
            //title = "А. С. Пушкин. Стихи, сочиненные ночью во время бессонницы. Читает Влада Гехтман";
            //shiftTitleLines = 2;
            //tempoCorrection = 0.0;
            //verses = true;
            //doOrigAlignRus(useWords, abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);


        }
    }
}
