﻿using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Google.Cloud.Storage.V1;
using Google.Apis.Storage.v1.Data;
using System.IO;

namespace Tool
{
    class GoogleTranscriber
    {
        const string projectId = "sylvan-road-817";
        const string bucketName = "sylvan-road-817-speech-bucket";
        const string objName = "recog-shite-object";
        readonly string fnCredential;

        public GoogleTranscriber(string fnCredential)
        {
            this.fnCredential = fnCredential;
        }

        public Material Transcribe(string fnAudio, string langCode)
        {
            // Keep storage client for full operation; will be removing file at the end
            StorageClient sc = null;
            Bucket bucket = null;
            try
            {
                sc = StorageClient.Create(GoogleCredential.FromFile(fnCredential));
                // Get out bucket, create on demand
                var buckets = sc.ListBuckets(projectId);
                foreach (var x in buckets) if (x.Name == bucketName) bucket = x;
                if (bucket == null) bucket = sc.CreateBucket(projectId, bucketName);
                // Kill all existing objects
                var objs = sc.ListObjects(bucketName);
                foreach (var x in objs) sc.DeleteObject(x);
                // Upload the damned thing
                using (var f = File.OpenRead(fnAudio))
                {
                    sc.UploadObject(bucketName, objName, null, f);
                }
                // NOW RECOGNIZE
                var mat = transcribeFromObject("gs://" + bucketName + "/" + objName, langCode);
                return mat;
            }
            finally
            {
                // Delete all objects in bucket
                if (bucket != null)
                {
                    var objs = sc.ListObjects(bucketName);
                    foreach (var x in objs) sc.DeleteObject(x);
                }
                // Adios storage jerk
                if (sc != null) sc.Dispose();
            }

        }

        Material transcribeFromObject(string gcsUri, string langCode)
        {
            SpeechClientBuilder scb = new SpeechClientBuilder();
            scb.CredentialsPath = fnCredential;
            var speech = scb.Build();
            //var audio = RecognitionAudio.FromFile(fnAudio);
            var audio = RecognitionAudio.FromStorageUri(gcsUri);
            var config = new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
                SampleRateHertz = 16000,
                LanguageCode = langCode,
                EnableAutomaticPunctuation = true,
                EnableWordTimeOffsets = true,
                ProfanityFilter = false,
            };
            var op = speech.LongRunningRecognize(config, audio);
            op = op.PollUntilCompleted();
            var response = op.Result;
            Material mat = new Material();
            foreach (var result in response.Results)
            {
                Segment segm = new Segment();
                var srAlt = result.Alternatives[0];
                for (int i = 0; i < srAlt.Words.Count; ++i)
                {
                    var srWord = srAlt.Words[i];
                    decimal startMSec = (decimal)Math.Round(srWord.StartTime.ToTimeSpan().TotalSeconds * 1000.0);
                    decimal endMSec = (decimal)Math.Round(srWord.EndTime.ToTimeSpan().TotalSeconds * 1000.0);
                    var word = new Word
                    {
                        StartSec = startMSec / 1000,
                        LengthSec = (endMSec - startMSec) / 1000,
                        Text = srWord.Word,
                    };
                    if (char.IsPunctuation(word.Text[word.Text.Length - 1]))
                    {
                        word.Trail = word.Text.Substring(word.Text.Length - 1);
                        word.Text = word.Text.Substring(0, word.Text.Length - 1);
                    }
                    segm.Words.Add(word);
                    if (word.Trail == "." || word.Trail == "?" || word.Trail == "!")
                    {
                        segm.StartSec = segm.Words[0].StartSec;
                        segm.LengthSec = segm.Words[segm.Words.Count - 1].StartSec + segm.Words[segm.Words.Count - 1].LengthSec - segm.StartSec;
                        mat.Segments.Add(segm);
                        segm = new Segment();
                    }
                }
                if (segm.Words.Count > 0)
                {
                    segm.StartSec = segm.Words[0].StartSec;
                    segm.LengthSec = segm.Words[segm.Words.Count - 1].StartSec + segm.Words[segm.Words.Count - 1].LengthSec - segm.StartSec;
                    mat.Segments.Add(segm);
                }
            }
            return mat;
        }
    }
}
