using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 43cefb870233460e8f3aa378600a7656

namespace MSTranscriber
{
    public class Program
    {
        /// <summary>
        /// This will be shown in Azure when transcription tasks are listed.
        /// </summary>
        const string taskDisplayName = "ListenClosely transcription";

        /// <summary>
        /// Config file to get transcription params from.
        /// </summary>
        const string cfgFileName = "TranscriptionConfig.json";

        static void Main(string[] args)
        {
            try
            {
                var cfgStr = System.IO.File.ReadAllText(cfgFileName);
                var cfg = JsonConvert.DeserializeObject<TranscriptionConfig>(cfgStr);

                RunAsync(cfg).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static async Task RunAsync(TranscriptionConfig cfg)
        {
            // create the client object and authenticate
            using (var client = BatchClient.CreateApiV3Client(cfg.SubscriptionKey, $"{cfg.Region}.api.cognitive.microsoft.com"))
            {
                await TranscribeAsync(client, cfg).ConfigureAwait(false);
            }
        }

        async static Task DeleteCompletedTranscriptions(BatchClient client)
        {
            Console.WriteLine("Deleting all existing completed transcriptions.");

            // get all transcriptions for the subscription
            PaginatedTranscriptions paginatedTranscriptions = null;
            do
            {
                if (paginatedTranscriptions == null)
                {
                    paginatedTranscriptions = await client.GetTranscriptionsAsync().ConfigureAwait(false);
                }
                else
                {
                    paginatedTranscriptions = await client.GetTranscriptionsAsync(paginatedTranscriptions.NextLink).ConfigureAwait(false);
                }

                // delete all pre-existing completed transcriptions. If transcriptions are still running or not started, they will not be deleted
                foreach (var transcriptionToDelete in paginatedTranscriptions.Values)
                {
                    // delete a transcription
                    await client.DeleteTranscriptionAsync(transcriptionToDelete.Self).ConfigureAwait(false);
                    Console.WriteLine($"Deleted transcription {transcriptionToDelete.Self}");
                }
            }
            while (paginatedTranscriptions.NextLink != null);

        }

        private async static Task TranscribeAsync(BatchClient client, TranscriptionConfig cfg)
        {
            if (cfg.DeleteOldTranscriptions)
            {
                await DeleteCompletedTranscriptions(client);
            }

            var newTranscription = new Transcription
            {
                DisplayName = taskDisplayName,
                Locale = cfg.Locale,
                ContentUrls = new[] { new Uri(cfg.WAVUrl) },
                Model = null,
                Properties = new TranscriptionProperties
                {
                    IsWordLevelTimestampsEnabled = true,
                    TimeToLive = TimeSpan.FromDays(1)
                }
            };

            newTranscription = await client.CreateTranscriptionAsync(newTranscription).ConfigureAwait(false);
            Console.WriteLine($"Created transcription {newTranscription.Self}");

            // get the transcription Id from the location URI
            var createdTranscriptions = new List<Uri> { newTranscription.Self };

            Console.WriteLine("Checking status.");

            // get the status of our transcriptions periodically and log results
            int completed = 0, running = 0, notStarted = 0;
            while (completed < 1)
            {
                completed = 0; running = 0; notStarted = 0;

                // get all transcriptions for the user
                PaginatedTranscriptions paginatedTranscriptions = null;
                do
                {
                    // <transcriptionstatus>
                    if (paginatedTranscriptions == null)
                    {
                        paginatedTranscriptions = await client.GetTranscriptionsAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        paginatedTranscriptions = await client.GetTranscriptionsAsync(paginatedTranscriptions.NextLink).ConfigureAwait(false);
                    }

                    // delete all pre-existing completed transcriptions. If transcriptions are still running or not started, they will not be deleted
                    foreach (var transcription in paginatedTranscriptions.Values)
                    {
                        switch (transcription.Status)
                        {
                            case "Failed":
                            case "Succeeded":
                                // we check to see if it was one of the transcriptions we created from this client.
                                if (!createdTranscriptions.Contains(transcription.Self))
                                {
                                    // not created form here, continue
                                    continue;
                                }

                                completed++;

                                // if the transcription was successful, check the results
                                if (transcription.Status == "Succeeded")
                                {
                                    var paginatedfiles = await client.GetTranscriptionFilesAsync(transcription.Links.Files).ConfigureAwait(false);

                                    var resultFile = paginatedfiles.Values.FirstOrDefault(f => f.Kind == ArtifactKind.Transcription);
                                    var result = await client.GetTranscriptionResultAsync(new Uri(resultFile.Links.ContentUrl)).ConfigureAwait(false);
                                    Console.WriteLine("Transcription succeeded.");
                                    using (StreamWriter sw = new StreamWriter(cfg.OutputFile))
                                    {
                                        sw.WriteLine(JsonConvert.SerializeObject(result, SpeechJsonContractResolver.WriterSettings));
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Transcription failed. Status: {0}", transcription.Properties.Error.Message);
                                }

                                break;

                            case "Running":
                                running++;
                                break;

                            case "NotStarted":
                                notStarted++;
                                break;
                        }
                    }

                    // for each transcription in the list we check the status
                    Console.WriteLine(string.Format("Transcriptions status: {0} completed, {1} running, {2} not started yet", completed, running, notStarted));
                }
                while (paginatedTranscriptions.NextLink != null);

                // check again after 1 minute
                await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            }
        }
    }
}
