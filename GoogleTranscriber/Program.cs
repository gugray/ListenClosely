﻿using System;

namespace GoogleTranscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            string ep = "APT_BKR_1";
            GoogleTranscriber gt = new GoogleTranscriber("ServiceAccountKey.json");
            gt.Transcribe("../_audio/" + ep + ".flac", "ru", "../_work/" + ep + "-conv-goog.json");
        }
    }
}
