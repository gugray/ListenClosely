﻿using System;
using System.IO;


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
            shiftSegments(mat, position, null, markAsTitle);
        }

        /**
         * This method will add an empty segment on the given position and add the "hidden" text into the new line
         */
        static void shiftSegments(Material mat, int position, string hiddenText, bool markAsTitle)
        {
            int i = mat.Segments.Count;

            // requested line position >= the end position - ? ...
            if (position >= mat.Segments[i - 1].ParaIx)
            {
                return;
            }

            i = mat.Segments.Count - 1;
            for (; i > 0; i--)
            {
                if (mat.Segments[i].ParaIx < position)
                {
                    break;
                }
            }
            i++;

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
            if(!string.IsNullOrEmpty(hiddenText))
            {
                mat.Segments[i].Words[0].Text = hiddenText;
                mat.Segments[i].IsHiddenTextLine = true;
            }
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

            AdditionalLines addPars = new AdditionalLines();

            String line;
            using (StreamReader sr = new StreamReader(addFn))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) return;
                    addPars.addLine(line);
                }
            }

            if(addPars.Lines.Count == 0)
            {
                return;
            }


            // add empty segments
            for (int i = 0; i < addPars.Lines.Count; i++)
            {
                AdditionalLines.AdditionalLine al = addPars.Lines[i];
                int addPar = al.Idx;
                if (shiftTitleLines > 0) addPar++;
                addPar += i;

                shiftSegments(mat, addPar, al.HiddenText, false);
            }
        }

        /**
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
        static void doOrigAlignRus(string abbreviation, decimal shift, double tempoCorrection, string customDictFileName,
            string title, int shiftTitleLines, bool verses, bool breakWork, bool useMS)
        {
            string transJson;
            Material trans = null;
            // Using Google?
            if (!useMS)
            {
                string googleJson = "_work/" + abbreviation + "-conv-goog.json";
                transJson = "_work/" + abbreviation + "-goog.json";

                // If transcription is missing, get it now
                if (!File.Exists(googleJson))
                {
                    // TODO
                    // Transcribe text with Google engine
                    GoogleTranscriber.GoogleTranscriber gt = new GoogleTranscriber.GoogleTranscriber("ServiceAccountKey.json");
                    gt.Transcribe("_audio/" + abbreviation + ".flac", "ru", googleJson); // ? "../_work/" + abbreviation + "-conv-goog.json"
                }

                // Set title, serialize
                trans = Material.fromGoogle(googleJson);
                trans.Title = title;
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

            dict.SortByLang();

            dict.indexDisplayedHeaders();

            // compose the lemmas-based translations
            dict.FillDict(mOrig);

            // Workaround for mark the title lines if required
            if(shiftTitleLines > 0)
            {
                shiftTitleSegments(mOrig, shiftTitleLines);
            }
            
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

        /**
         * 
         * */
        static void printDownloadEntry(String abbreviation, String title, String baseUrl)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("<b>" + title + "</b>");
            Console.WriteLine("<a href = \"" + baseUrl + "/media/" + abbreviation + "-segs.json\">JSON</a>");
            Console.WriteLine("<a href = \"" + baseUrl + "/media/" + abbreviation + ".m4a\">M4A</a>");
            Console.WriteLine("<a href = \"" + baseUrl + "/media/" + abbreviation + ".webm\">WEBM</a>");
        }
        static void printIndexEntry(String abbreviation, String title, String baseUrl)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("<li class=\"title\">");
            Console.WriteLine("<a href=\"" + baseUrl + "/prose/player.html?ep=" + abbreviation  + "\">" + title + "</a>");
            Console.WriteLine("</li>");
        }

        static void Main(string[] args)
        {
            string customDictFileName = "ru-custom.txt";

            bool breakWork = false;  // for 1st start, set true; for 2nd start, set false

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
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_ANS_2";
            // title = "А. П. Чехов. Анна на шее (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_ANS_3";
            // title = "А. П. Чехов. Анна на шее (3). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_ANS_4";
            // title = "А. П. Чехов. Анна на шее (4). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //  
            // abbreviation = "ATCH_GFR";
            // title = "А. П. Чехов. Глупый француз. Читает Владимир Иванчин";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "LTL_RTD";
            // title = "Лев Толстой. Детство. Читает Анна Шибарова";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "LTL_VIM";
            // title = "Лев Толстой: Война и мир. Читает Анна Шибарова";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "ATCH_STU_1";
            // title = "А. П. Чехов. Студент (1). Читает Анна Шибарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_STU_2";
            // title = "А. П. Чехов. Студент (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_1";
            // title = "А. С. Пушкин. Метель (1). Читает Анна Шибарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_2";
            // title = "А. С. Пушкин. Метель (2). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_3";
            // title = "А. С. Пушкин. Метель (3). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_4";
            // title = "А. С. Пушкин. Метель (4). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_MET_5";
            // title = "А. С. Пушкин. Метель (5). Читает Анна Шибарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "APT_BKR_1";
            // title = "А. С. Пушкин. Барышня-крестьянка (1). Читает Влада Гехтман";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "APT_BKR_2";
            // title = "А. С. Пушкин. Барышня-крестьянка (2). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_3";
            // title = "А. С. Пушкин. Барышня-крестьянка (3). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_4";
            // title = "А. С. Пушкин. Барышня-крестьянка (4). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BKR_5";
            // title = "А. С. Пушкин. Барышня-крестьянка (5). Читает Влада Гехтман";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "ATCH_SHT_1";
            // title = "А. П. Чехов (1). Шуточка. Читает Марина Бобрик";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "ATCH_SHT_2";
            // title = "А. П. Чехов (2). Шуточка. Читает Марина Бобрик";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_PIN_1";
            // title = "Федор Достоевский (1). Преступление и наказание. Часть четвертая. Глава четвертая. Читает Айна Любарова";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_PIN_2";
            // title = "Федор Достоевский (2). Преступление и наказание. Часть четвертая. Глава четвертая. Читает Айна Любарова";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_ELK_1";
            // title = "Федор Достоевский (1). Ёлка и свадьба (Из записок неизвестного).Читает Илья Кукуй";
            // shiftTitleLines = 3;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_ELK_2";
            // title = "Федор Достоевский (2). Ёлка и свадьба (Из записок неизвестного).Читает Илья Кукуй";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "MLE_PAR";
            // title = "Михаил Лермонтов. Парус. Читает Вениамин Ицкович";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_GOV";
            // title = "М. Ю. Лермонтов. Из Гете. Читает Даниил Казбеков";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_PNT";
            // title = "М. Ю. Лермонтов. Посреди небесных тел... Читает Михаил Казбеков";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_VOD";
            // title = "М. Ю. Лермонтов. Выхожу один я на дорогу... Читает Даниил Казбеков";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_DMJ";
            // title = "А. С. Пушкин. В альбом Павлу Вяземскому. Читает Михаил Казбеков";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_EZH";
            // title = "А. С. Пушкин. Если жизнь тебя обманет. Читает Михаил Казбеков";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_SSN";
            // title = "А. С. Пушкин. Стихи, сочиненные ночью во время бессонницы. Читает Влада Гехтман";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_BESY";
            // title = "Александр Пушкин. Бесы. Читает Александр Заполь";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_ZAV";
            // title = "Михаил Лермонтов. Завещание. Читает Евгений Шибаров";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "MLE_PAR_1";
            // title = "Михаил Лермонтов. Парус. Читает Евгений Шибаров";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //   
            // abbreviation = "MLE_ROD";
            // title = "Михаил Лермонтов. Родина. Читает Евгений Шибаров";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //  
            // abbreviation = "MLE_UTES";
            // title = "Михаил Лермонтов. Утес. Читает Даниил Казбеков";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "APT_ONEG_1";
            // title = "Александр Пушкин. Роман в стихах «Евгений Онегин». Глава шестая. Строфы XXX - XXXIII. Читает Евгений Шибаров";
            // shiftTitleLines = 4;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "BLOK_DEV";
            // title = "Александр Блок. Девушка пела в церковном хоре... Читает Елена Грачева";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "BLOK_NOCH";
            // title = "Александр Блок. Ночь, улица, фонарь, аптека... Читает Дмитрий Калугин";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "BLOK_OPM";
            // title = "Александр Блок. Она пришла с мороза... Читает Вениамин Ицкович";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "BLOK_VOR";
            // title = "Александр Блок. Ворона. Читает Эля Любарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FET_KART";
            // title = "Афанасий Фет. Чудная картина... Читает Алексей Востриков";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FET_KOT";
            // title = "Афанасий Фет. Кот поет, глаза прищуря... Читает Влада Гехтман";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FET_NOK";
            // title = "Афанасий Фет. Непогода - осень - куришь... Читает Вениамин Ицкович";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FET_YDSN";
            // title = "Афанасий Фет. Я долго стоял неподвижно Читает Алексей Востриков";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FET_ZAR";
            // title = "Афанасий Фет. На заре ты её не буди... Читает Вениамин Ицкович";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_VUAL";
            // title = "Анна Ахматова. Сжала руки под темной вуалью... Читает Любовь Шендерова";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_DANTE";
            // title = "Анна Ахматова. Данте. Читает Айна Любарова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_MOL";
            // title = "Анна Ахматова. Молюсь оконному лучу... Читает Анна Зиндер";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_MUSA";
            // title = "Анна Ахматова. Муза. Читает Наталья Мовнина";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_MUSE";
            // title = "Анна Ахматова. Музе. Читает Любовь Шендерова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_ONLJB";
            // title = "Анна Ахматова. Он любил три вещи на свете... Читает Любовь Шендерова";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_PESENKA";
            // title = "Анна Ахматова. Песенка. Читает Любовь Шендерова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_PPVSTR";
            // title = "Анна Ахматова. Песня последней встречи. Читает Любовь Шендерова";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_PROVDR";
            // title = "Анна Ахматова. Проводила друга до передней... Читает Анна Зиндер";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_PRGLK";
            // title = "Анна Ахматова. Прогулка. Читает Наталья Мовнина";
            // shiftTitleLines = 2;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_KAKVS";
            // title = "Анна Ахматова. Хочешь знать, как всё это было?.. Читает Анна Зиндер";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "AHM_KAKKUK";
            // title = "Анна Ахматова. Я живу, как кукушка в часах... Читает Любовь Шендерова";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "AHM_SHSUM";
            // title = "Анна Ахматова. Я сошла с ума, о мальчик странный... Читает Любовь Шендерова";
            // shiftTitleLines = 1;
            // tempoCorrection = 0.0;
            // verses = true;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            //
            // abbreviation = "FMD_PIN_3";
            // title = "Федор Достоевский (1). Преступление и наказание. Часть первая. Глава первая. Читает Дмитрий Калугин";
            // shiftTitleLines = 5;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_PIN_4";
            // title = "Федор Достоевский (2). Преступление и наказание. Часть первая. Глава первая. Читает Дмитрий Калугин";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_PIN_5";
            // title = "Федор Достоевский (3). Преступление и наказание. Часть первая. Глава первая. Читает Дмитрий Калугин";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            // abbreviation = "FMD_PIN_6";
            // title = "Федор Достоевский (4). Преступление и наказание. Часть первая. Глава первая. Читает Дмитрий Калугин";
            // shiftTitleLines = 0;
            // tempoCorrection = 0.0;
            // verses = false;
            // doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
            // 
            abbreviation = "FMD_PIN_7";
            title = "Федор Достоевский (5). Преступление и наказание. Часть первая. Глава первая. Читает Дмитрий Калугин";
            shiftTitleLines = 0;
            tempoCorrection = 0.0;
            verses = false;
            doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);

        }
    }
}
