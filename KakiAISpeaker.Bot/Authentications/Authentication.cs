using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KakiAISpeaker.Bot.Authentications
{
    /// <summary>
    /// 서비스 인증
    /// </summary>
    public class Authentication
    {
        //https://docs.microsoft.com/ko-kr/azure/cognitive-services/speech-service/rest-apis

        public static readonly string FetchTokenUri =
            "https://eastasia.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        // Access token expires every 10 minutes. Renew it every 9 minutes.
        private const int REFRESH_TOKEN_DURATION = 9;
        private readonly Timer _accessTokenRenewer;
        private readonly string _subscriptionKey;
        private string _token;

        public Authentication(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
            _token = FetchToken(FetchTokenUri, subscriptionKey).Result;

            // renew the token on set duration.
            _accessTokenRenewer = new Timer(OnTokenExpiredCallback,
                this,
                TimeSpan.FromMinutes(REFRESH_TOKEN_DURATION),
                TimeSpan.FromMilliseconds(-1));
        }

        public string GetAccessToken()
        {
            return _token;
        }

        private void RenewAccessToken()
        {
            _token = FetchToken(FetchTokenUri, _subscriptionKey).Result;
            Console.WriteLine("Renewed token.");
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed renewing access token. Details: {0}", ex.Message);
            }
            finally
            {
                try
                {
                    _accessTokenRenewer.Change(TimeSpan.FromMinutes(REFRESH_TOKEN_DURATION),
                        TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message);
                }
            }
        }

        private static async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                var uriBuilder = new UriBuilder(fetchUri);

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                Console.WriteLine("Token Uri: {0}", uriBuilder.Uri.AbsoluteUri);
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}