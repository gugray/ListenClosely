using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Google.Cloud.Storage.V1;
using Google.Apis.Storage.v1.Data;
using Newtonsoft.Json;
using System.Linq;

namespace GoogleTranscriber
{
    public class GoogleTranscriber
    {
        private readonly string projectId;
        private readonly string bucketName;
        private readonly string fnCredential;
        readonly static string OBJ_NAME = "recog-shite-object";

        public GoogleTranscriber(string fnCredential, string projectId, string bucketName)
        {
            this.fnCredential = fnCredential;
            this.projectId = projectId;
            this.bucketName = bucketName;
        }

        public void Transcribe(string fnAudio, string langCode, string fnOut)
        {
            // Keep storage client for full operation; will be removing file at the end
            StorageClient sc = null;
            Bucket bucket = null;
            try
            {
                GoogleCredential cred = GoogleCredential.FromFile(fnCredential);
                sc = StorageClient.Create(cred);
                // Get out bucket, create on demand
                var buckets = sc.ListBuckets(this.projectId);
                if(buckets.Count() > 0)
                {
                    foreach (var x in buckets)
                    {
                        if (x.Name == this.bucketName)
                        {
                            bucket = x;
                            break;
                        }
                    }
                }
                if (bucket == null) bucket = sc.CreateBucket(this.projectId, this.bucketName);
                // Kill all existing objects
                var objs = sc.ListObjects(this.bucketName);
                foreach (var x in objs) sc.DeleteObject(x);
                // Upload the damned thing
                using (var f = File.OpenRead(fnAudio))
                {
                    sc.UploadObject(this.bucketName, OBJ_NAME, null, f);
                }
                // NOW RECOGNIZE
                transcribeFromObject("gs://" + this.bucketName + "/" + OBJ_NAME, langCode, fnOut);
            }
            finally
            {
                // Delete all objects in bucket
                if (bucket != null)
                {
                    var objs = sc.ListObjects(this.bucketName);
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
