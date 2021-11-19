using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Google.Cloud.Storage.V1;
using Google.Apis.Storage.v1.Data;
using Newtonsoft.Json;

namespace GoogleTranscriber
{
    public class GoogleTranscriber
    {
        const string projectId = "sylvan-road-817";
        const string bucketName = "sylvan-road-817-speech-bucket";
        const string objName = "recog-shite-object";
        readonly string fnCredential;

        public GoogleTranscriber(string fnCredential)
        {
            this.fnCredential = fnCredential;
        }

        public void Transcribe(string fnAudio, string langCode, string fnOut)
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
                transcribeFromObject("gs://" + bucketName + "/" + objName, langCode, fnOut);
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

        void transcribeFromObject(string gcsUri, string langCode, string fnOut)
        {
            SpeechClientBuilder scb = new SpeechClientBuilder();
            scb.CredentialsPath = fnCredential;
            var speech = scb.Build();
            //var audio = RecognitionAudio.FromFile(fnAudio);
            var audio = RecognitionAudio.FromStorageUri(gcsUri);
            var config = new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
                // SampleRateHertz = 16000,
                LanguageCode = langCode,
                EnableAutomaticPunctuation = true,
                EnableWordTimeOffsets = true,
                ProfanityFilter = false,
                // AudioChannelCount = 2,
            };
            var op = speech.LongRunningRecognize(config, audio);
            op = op.PollUntilCompleted();
            var response = op.Result;
            var responseJson = JsonConvert.SerializeObject(response);
            File.WriteAllText(fnOut, responseJson);
        }
    }
}
