using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Twitterizer;

namespace Titter
{
    public class TwitterSender
    {
        private readonly string consumerKey;
        private readonly string consumerSecret;
        private readonly string oauthToken;
        private readonly string oauthTokenSecret;

        private const string headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
              "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
              "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
              "oauth_version=\"{6}\"";

        public TwitterSender(string consumer_key, string consumer_secret, string oauth_token, string oauth_token_secret)
        {
            consumerKey = consumer_key;
            consumerSecret = consumer_secret;
            oauthToken = oauth_token;
            oauthTokenSecret = oauth_token_secret;
        }

        public string SendDirectMessage(string user, string text)
        {
            string post_data;
            string resource_url;

            string authHeader = GetPostDirectMessageBaseString(text, user, oauthToken, oauthTokenSecret, out post_data, out resource_url);
            return Send(resource_url, post_data, authHeader);
        }

        public string SendTwit(string text)
        {
            string post_data;
            string resource_url;

            string authHeader = GetStatusBaseString(text, oauthToken, oauthTokenSecret, out post_data, out resource_url);
            return Send(resource_url, post_data, authHeader);
        }

        public string SendPictureTwit(string text, byte[] picture)
        {
            var accesstoken = new OAuthTokens
                {
                AccessToken =oauthToken,
                AccessTokenSecret = oauthTokenSecret,
                ConsumerKey = consumerKey,
                ConsumerSecret = consumerSecret
            };
            var response = TwitterStatus.UpdateWithMedia(accesstoken, text, picture);
            return response.Result == RequestResult.Success ? "200" : "403"; 
        }

        private static string Send(string resource_url, string post_data, string auth_header)
        {
            var request = (HttpWebRequest)WebRequest.Create(resource_url);
            request.Headers.Add("Authorization", auth_header);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = post_data.Length;

            using (var stream = request.GetRequestStream())
            {
                byte[] content = Encoding.ASCII.GetBytes(post_data);
                stream.Write(content, 0, content.Length);
            }
            try
            {
                request.GetResponse();
                return "200";
            }
            catch (WebException)
            {
                return "403";
            }
        }

        private string GetAuthHeader(string oauth_token, string oauth_token_secret, string post_data, string resource_url)
        {
            const string oauth_version = "1.0";
            const string oauth_signature_method = "HMAC-SHA1";
            var oauth_nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)));
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            var baseFormat = "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
                "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}&" + post_data;

            var baseString = string.Format(baseFormat,
                                        consumerKey,
                                        oauth_nonce,
                                        oauth_signature_method,
                                        oauth_timestamp,
                                        oauth_token,
                                        oauth_version
                                        );

            baseString = string.Concat("POST&", Uri.EscapeDataString(resource_url),
                         "&", Uri.EscapeDataString(baseString));

            var compositeKey = string.Concat(Uri.EscapeDataString(consumerSecret),
                        "&", Uri.EscapeDataString(oauth_token_secret));

            string oauth_signature;
            using (var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(compositeKey)))
            {
                oauth_signature = Convert.ToBase64String(
                    hasher.ComputeHash(Encoding.ASCII.GetBytes(baseString)));
            }

            var authHeader = string.Format(headerFormat,
                                    Uri.EscapeDataString(oauth_nonce),
                                    Uri.EscapeDataString(oauth_signature_method),
                                    Uri.EscapeDataString(oauth_timestamp),
                                    Uri.EscapeDataString(consumerKey),
                                    Uri.EscapeDataString(oauth_token),
                                    Uri.EscapeDataString(oauth_signature),
                                    Uri.EscapeDataString(oauth_version)
                            );

            return authHeader;
        }

        private string GetStatusBaseString(string status, string oauth_token, string oauth_token_secret, out string post_data, out string resource_url)
        {
            post_data = "status=" + Uri.EscapeDataString(status);
            resource_url = "https://api.twitter.com/1.1/statuses/update.json";

            return GetAuthHeader(oauth_token, oauth_token_secret, post_data, resource_url);
        }

        private string GetPostDirectMessageBaseString(string text, string screen_name, string oauth_token, string oauth_token_secret, out string post_data, out string resource_url)
        {
            post_data = "screen_name=" + Uri.EscapeDataString(screen_name) + "&text=" + Uri.EscapeDataString(text);
            resource_url = "https://api.twitter.com/1.1/direct_messages/new.json";

            return GetAuthHeader(oauth_token, oauth_token_secret, post_data, resource_url);
        }
    }
}
