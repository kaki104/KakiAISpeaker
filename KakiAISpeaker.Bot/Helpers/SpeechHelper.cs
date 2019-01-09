using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KakiAISpeaker.Bot.Authentications;
using KakiAISpeaker.Bot.Models;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KakiAISpeaker.Bot.Helpers
{
    public class SpeechHelper
    {
        // private const string LOCALE = "en-us";
        private const string LOCALE = "ko-KR";

        private readonly Authentication _authentication;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _requestUri;

        /// <summary>
        ///     런타임 생성자
        /// </summary>
        public SpeechHelper(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            //https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/rest-apis
            //https://docs.microsoft.com/ko-kr/azure/cognitive-services/speech-service/rest-apis

            _httpClientFactory = httpClientFactory;

            _requestUri =
                $@"https://eastasia.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={
                        LOCALE
                    }";
            _authentication =
                new Authentication(configuration.GetSection("speechServiceSubscriptionKey")?.Value);
        }

        /// <summary>
        ///     오디오 파일에서 택스트 반환 - 로컬 테스트용
        /// </summary>
        /// <param name="fileFullPath"></param>
        /// <returns></returns>
        public async Task<SpeechResult> GetSpeechResultFromAudioAsync(string fileFullPath)
        {
            if (string.IsNullOrEmpty(fileFullPath)) return null;

            var stream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);

            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/xml"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authentication.GetAccessToken());

                var content = new StreamContent(stream);
                content.Headers.Add("ContentType", new[] {"audio/wav", "codec=audio/pcm", "samplerate=16000"});

                try
                {
                    var response = await client.PostAsync(_requestUri, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var speechResults = JsonConvert.DeserializeObject<SpeechResult>(responseContent);
                    return speechResults;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                finally
                {
                    content.Dispose();
                    stream.Dispose();
                }
            }

            return null;
        }

        /// <summary>
        ///     Returning speech result from attachment
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public async Task<SpeechResult> GetSpeechResultFromAttachmentAsync(Attachment attachment)
        {
            if (attachment == null) return null;

            var splits = attachment.ContentUrl.Split(',');
            var bytes = Convert.FromBase64String(splits[1]);
            var memoryStream = new MemoryStream(bytes);
            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/xml"));
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _authentication.GetAccessToken());

                    var content = new StreamContent(memoryStream);
                    content.Headers.Add("ContentType", new[] {"audio/wav", "codec=audio/pcm", "samplerate=16000"});

                    try
                    {
                        var response = await client.PostAsync(_requestUri, content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var speechResults = JsonConvert.DeserializeObject<SpeechResult>(responseContent);
                        return speechResults;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                    finally
                    {
                        content.Dispose();
                        memoryStream.Flush();
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}