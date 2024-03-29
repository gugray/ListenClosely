using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace MSTranscriber
{
    public partial class BatchClient : IDisposable
    {
        private const int MaxNumberOfRetries = 5;

        private readonly HttpClient client;
        private readonly string speechToTextBasePath;

        private static AsyncRetryPolicy<HttpResponseMessage> transientFailureRetryingPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(x => !x.IsSuccessStatusCode && (int)x.StatusCode == 429)
            .WaitAndRetryAsync(MaxNumberOfRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Request failed with {result.Exception?.ToString() ?? result.Result?.StatusCode.ToString()}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
            });

        private BatchClient(HttpClient client)
        {
            this.client = client;
            speechToTextBasePath = "speechtotext/v3.0/";
        }

        public static BatchClient CreateApiV3Client(string key, string hostName)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(25);
            client.BaseAddress = new UriBuilder(Uri.UriSchemeHttps, hostName).Uri;

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            return new BatchClient(client);
        }

        private async Task<TResponse> PostAsJsonAsync<TPayload, TResponse>(string path, TPayload payload)
        {
            string json = JsonConvert.SerializeObject(payload, SpeechJsonContractResolver.WriterSettings);
            StringContent content = new StringContent(json);
            content.Headers.ContentType = JsonMediaTypeFormatter.DefaultMediaType;

            var response = await transientFailureRetryingPolicy
                .ExecuteAsync(() => this.client.PostAsync(path, content))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<TResponse>(
                    new[]
                    {
                        new JsonMediaTypeFormatter
                        {
                            SerializerSettings = SpeechJsonContractResolver.ReaderSettings
                        }
                    }).ConfigureAwait(false);
            }

            throw await CreateExceptionAsync(response).ConfigureAwait(false);
        }

        private async Task<TResponse> GetAsync<TResponse>(string path)
        {
            var response = await transientFailureRetryingPolicy
                .ExecuteAsync(async () => await this.client.GetAsync(path).ConfigureAwait(false))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<TResponse>().ConfigureAwait(false);

                return result;
            }

            throw await CreateExceptionAsync(response);
        }

        private static async Task<FailedHttpClientRequestException> CreateExceptionAsync(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    return new FailedHttpClientRequestException(response.StatusCode, "No permission to access this resource.");
                case HttpStatusCode.Unauthorized:
                    return new FailedHttpClientRequestException(response.StatusCode, "Not authorized to see the resource.");
                case HttpStatusCode.NotFound:
                    return new FailedHttpClientRequestException(response.StatusCode, "The resource could not be found.");
                case HttpStatusCode.UnsupportedMediaType:
                    return new FailedHttpClientRequestException(response.StatusCode, "The file type isn't supported.");
                case HttpStatusCode.BadRequest:
                    {
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var shape = new { Message = string.Empty };
                        var result = JsonConvert.DeserializeAnonymousType(content, shape);
                        if (result != null && !string.IsNullOrEmpty(result.Message))
                        {
                            return new FailedHttpClientRequestException(response.StatusCode, result.Message);
                        }

                        return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
                    }

                default:
                    return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
            }
        }

        public Task<PaginatedTranscriptions> GetTranscriptionsAsync()
        {
            var path = $"{this.speechToTextBasePath}transcriptions";
            return this.GetAsync<PaginatedTranscriptions>(path);
        }

        public Task<PaginatedTranscriptions> GetTranscriptionsAsync(Uri location)
        {
            return this.GetAsync<PaginatedTranscriptions>(location.PathAndQuery);
        }

        public Task<PaginatedFiles> GetTranscriptionFilesAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return this.GetAsync<PaginatedFiles>(location.PathAndQuery);
        }

        public Task<Transcription> GetTranscriptionAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return this.GetAsync<Transcription>(location.PathAndQuery);
        }

        public async Task<RecognitionResults> GetTranscriptionResultAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var response = await transientFailureRetryingPolicy
                .ExecuteAsync(async () => await this.client.GetAsync(location).ConfigureAwait(false))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<RecognitionResults>(json, SpeechJsonContractResolver.ReaderSettings);
            }

            throw await CreateExceptionAsync(response);
        }

        public Task<Transcription> CreateTranscriptionAsync(Transcription transcription)
        {
            if (transcription == null)
            {
                throw new ArgumentNullException(nameof(transcription));
            }

            var path = $"{this.speechToTextBasePath}transcriptions/";

            return this.PostAsJsonAsync<Transcription, Transcription>(path, transcription);
        }

        public Task DeleteTranscriptionAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return transientFailureRetryingPolicy
                .ExecuteAsync(() => this.client.DeleteAsync(location.PathAndQuery));
        }

        public void Dispose()
        {
            this.client?.Dispose();
        }
    }
}
