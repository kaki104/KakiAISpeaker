using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.Extensions.Configuration;

namespace KakiAISpeaker.Bot.Helpers
{
    /// <summary>
    /// 폴리 서비스 헬퍼
    /// </summary>
    public class PollyHelper
    {
        private readonly AmazonPollyClient _pollyClient;

        public PollyHelper(IConfiguration configuration)
        {
            //폴리 클라이언트 생성
            _pollyClient = new AmazonPollyClient(
                configuration.GetSection("awsAccessKeyId").Value,
                configuration.GetSection("awsSecretAccessKey").Value,
                RegionEndpoint.APNortheast2);
        }

        /// <summary>
        ///     Making Voice Using Poly Service
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<SynthesizeSpeechResponse> GetSynthesizeSpeechFromTextAsync(string text)
        {
            if (_pollyClient == null
                || string.IsNullOrEmpty(text)) return null;

            var request = new SynthesizeSpeechRequest
            {
                Text = $"<speak>{text}</speak>",
                OutputFormat = OutputFormat.Mp3,
                VoiceId = VoiceId.Seoyeon,
                LanguageCode = "ko-KR",
                TextType = TextType.Ssml
            };

            try
            {
                var response = await _pollyClient.SynthesizeSpeechAsync(request);
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}