using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Titter
{
    public class TwitterUpdateWithMedia
    {
        private readonly string consumerKey;
        private readonly string consumerSecret;
        private readonly string oauthToken;
        private readonly string oauthTokenSecret;
        private const string url = "https://api.twitter.com/1.1/statuses/update_with_media.json";
        private byte[] formData;
        public const string Realm = "Twitter API";
        public Dictionary<string, object> Parameters { get; private set; }
        private static readonly string[] OAuthParametersToIncludeInHeader = new[]
                                                          {
                                                              "oauth_version",
                                                              "oauth_nonce",
                                                              "oauth_timestamp",
                                                              "oauth_signature_method",
                                                              "oauth_consumer_key",
                                                              "oauth_token"
                                                          };
        private static readonly string[] SecretParameters = new[]
                                                                {
                                                                    "oauth_consumer_secret",
                                                                    "oauth_token_secret",
                                                                    "oauth_signature"
                                                                };

        public TwitterUpdateWithMedia(string consumer_key, string consumer_secret, string oauth_token, string oauth_token_secret)
        {
            consumerKey = consumer_key;
            consumerSecret = consumer_secret;
            oauthToken = oauth_token;
            oauthTokenSecret = oauth_token_secret;
            Parameters = new Dictionary<string, object>();
        }

        public string UpdateWithMedia(string status, byte[] image)
        {
            var request = PrepareRequest(status, image);

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

        private HttpWebRequest PrepareRequest(string status, IEnumerable image)
        {
            SetupOAuth();
            Parameters.Add("status", status);
            Parameters.Add("media[]", image);

            formData = null;
            const string boundary = "--------------------f0Pe13";
            const string contentType = "multipart/form-data; boundary=" + boundary;

            formData = GetMultipartFormData(Parameters, boundary);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.AutomaticDecompression = DecompressionMethods.None;
            request.UseDefaultCredentials = true;
            request.Method = "POST";
            request.ContentLength = formData.Length;
            //request.UserAgent = "Titter webRunes";
            request.ServicePoint.Expect100Continue = false;
            request.Headers.Add("Authorization", GenerateAuthorizationHeader());
            request.ContentType = contentType;


            using (var requestStream = request.GetRequestStream())
            {
                if (formData != null)
                {
                    requestStream.Write(formData, 0, formData.Length);
                }
            }

            return request;
        }

        private void SetupOAuth()
        {
            Parameters.Add("oauth_version", "1.0");
            Parameters.Add("oauth_nonce", GenerateNonce());
            Parameters.Add("oauth_timestamp", GenerateTimeStamp());
            Parameters.Add("oauth_signature_method", "HMAC-SHA1");
            Parameters.Add("oauth_consumer_key", consumerKey);
            Parameters.Add("oauth_consumer_secret", consumerSecret);
            Parameters.Add("oauth_token", oauthToken);
            Parameters.Add("oauth_token_secret", oauthTokenSecret);

            var signature = GenerateSignature();

            Parameters.Add("oauth_signature", signature);
        }

        private static string GenerateNonce()
        {
            return new Random()
                .Next(123400, int.MaxValue)
                .ToString("X", CultureInfo.InvariantCulture);
        }

        private static string GenerateTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
        }

        private string GenerateSignature()
        {
            var nonSecretParameters = (from p in Parameters
                                               where (!SecretParameters.Contains(p.Key) && p.Key.StartsWith("oauth_"))
                                               select p);

            var signatureBaseString = string.Format(
                CultureInfo.InvariantCulture,
                "{0}&{1}&{2}",
                "POST", UrlEncode(url), UrlEncode(nonSecretParameters));

            var key = string.Format(
                CultureInfo.InvariantCulture,
                "{0}&{1}",
                UrlEncode(consumerSecret),
                UrlEncode(oauthTokenSecret));


            // Generate the hash
            var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            var signatureBytes = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBaseString));
            return Convert.ToBase64String(signatureBytes);
        }

        private static string UrlEncode(string value)
        {
            value = Uri.EscapeDataString(value);
            value = Regex.Replace(value, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());

            value = value
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27");

            value = value.Replace("%7E", "~");

            return value;
        }

        private static string UrlEncode(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var parameterString = new StringBuilder();

            var paramsSorted = from p in parameters
                               orderby p.Key, p.Value
                               select p;

            foreach (var item in paramsSorted)
            {
                var s = item.Value as string;
                if (s != null)
                {
                    if (parameterString.Length > 0)
                    {
                        parameterString.Append("&");
                    }

                    parameterString.Append(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}={1}",
                            UrlEncode(item.Key),
                            UrlEncode(s)));
                }
            }

            return UrlEncode(parameterString.ToString());
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> param, string boundary)
        {
            Stream formDataStream = new MemoryStream();
            var encoding = Encoding.UTF8;

            var fieldsToInclude = new Dictionary<string, object>(param.Where(p => !OAuthParametersToIncludeInHeader.Contains(p.Key) &&
                             !SecretParameters.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));

            foreach (var kvp in fieldsToInclude)
            {
                if (kvp.Value.GetType() == typeof(byte[]))
                {	
                    var data = (byte[])kvp.Value;
                    var header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: application/octet-stream\r\n\r\n",
                        boundary,
                        kvp.Key,
                        kvp.Key);

                    var headerBytes = encoding.GetBytes(header);

                    formDataStream.Write(headerBytes, 0, headerBytes.Length);
                    formDataStream.Write(data, 0, data.Length);
                }
                else
                {	
                    var header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
                        boundary,
                        kvp.Key,
                        kvp.Value);

                    var headerBytes = encoding.GetBytes(header);

                    formDataStream.Write(headerBytes, 0, headerBytes.Length);
                }
            }

            var footer = string.Format("\r\n--{0}--\r\n", boundary);
            formDataStream.Write(encoding.GetBytes(footer), 0, footer.Length);
            formDataStream.Position = 0;
            var tempFormData = new byte[formDataStream.Length];

            formDataStream.Read(tempFormData, 0, tempFormData.Length);
            formDataStream.Close();

            return tempFormData;
        }

        private string GenerateAuthorizationHeader()
        {
            var authHeaderBuilder = new StringBuilder();
            authHeaderBuilder.AppendFormat("OAuth realm=\"{0}\"", Realm);

            var sortedParameters = from p in Parameters
                                   where OAuthParametersToIncludeInHeader.Contains(p.Key)
                                   orderby p.Key, UrlEncode((p.Value is string) ? (string)p.Value : string.Empty)
                                   select p;

            foreach (var item in sortedParameters)
            {
                authHeaderBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value as string));
            }

            authHeaderBuilder.AppendFormat(",oauth_signature=\"{0}\"", UrlEncode(Parameters["oauth_signature"] as string));

            return authHeaderBuilder.ToString();
        }

    }
}
