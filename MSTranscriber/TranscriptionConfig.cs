using System;
using System.Collections.Generic;
using System.Text;

namespace MSTranscriber
{
    public class TranscriptionConfig
    {
        public string SubscriptionKey { get; set; }
        public string Region { get; set; }
        public bool DeleteOldTranscriptions { get; set; }
        public string WAVUrl { get; set; }
        public string Locale { get; set; }
        public string OutputFile { get; set; }
    }
}
