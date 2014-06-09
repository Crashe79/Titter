using System;
using System.IO;
using System.Net;
using System.Text;

namespace Titter
{
    public class TitterService
    {
        private readonly string consumerKey;
        private readonly string consumerSecret;

        public TitterService(string cunsomerKey, string cunsomerSecret)
        {
            consumerKey = cunsomerKey;
            consumerSecret = cunsomerSecret;
        }

        public TwitterToken GetRequestToken()
        {
            var uri = new Uri("https://api.twitter.com/oauth/request_token");
            var oAuth = new OAuthBase();
            var timeStamp = oAuth.GenerateTimeStamp();
            var nonce = oAuth.GenerateNonce();
            string normUri;
            string normParams;
            var sig = oAuth.GenerateSignature(uri, consumerKey, consumerSecret, string.Empty, string.Empty,
                "GET", timeStamp, nonce, OAuthBase.SignatureTypes.HMACSHA1, out normUri, out normParams);
            var request_url =
              "https://api.twitter.com/oauth/request_token" + "?" +
              "oauth_consumer_key=" + consumerKey + "&" +
              "oauth_signature_method=" + "HMAC-SHA1" + "&" +
              "oauth_signature=" + sig + "&" +
              "oauth_timestamp=" + timeStamp + "&" +
              "oauth_nonce=" + nonce + "&" +
              "oauth_version=" + "1.0";

            var twitterToken = new TwitterToken();
            var Request = (HttpWebRequest)WebRequest.Create(request_url);
            try
            {
                var Response = (HttpWebResponse)Request.GetResponse();
                using (var Reader = new StreamReader(Response.GetResponseStream(), Encoding.GetEncoding(1251)))
                {
                    var outline = Reader.ReadToEnd();
                    char[] delimiterChars = { '&', '=' };
                    var words = outline.Split(delimiterChars);
                    twitterToken.OauthToken = words[1];
                    twitterToken.OauthTokenSecret = words[3];
                    var oauth_callback_confirmed = words[5];

                    return twitterToken;
                }
                
            }
            catch (Exception)
            {
                return twitterToken;
            }
        }

        public TwitterToken GetOauthToken(string verifier, string token)
        {
            var uri = new Uri("https://api.twitter.com/oauth/request_token");
            var oAuth = new OAuthBase();
            var timeStamp = oAuth.GenerateTimeStamp();
            var nonce = oAuth.GenerateNonce();
            string normUri;
            string normParams;
            var sig = oAuth.GenerateSignature(uri, consumerKey, consumerSecret, string.Empty, string.Empty,
                "GET", timeStamp, nonce, OAuthBase.SignatureTypes.HMACSHA1, out normUri, out normParams);
            var request_url =
              "https://api.twitter.com/oauth/access_token" + "?" +
              "oauth_consumer_key=" + consumerKey + "&" +
              "oauth_token=" + token + "&" +
              "oauth_signature_method=" + "HMAC-SHA1" + "&" +
              "oauth_signature=" + sig + "&" +
              "oauth_timestamp=" + timeStamp + "&" +
              "oauth_nonce=" + nonce + "&" +
              "oauth_version=" + "1.0" + "&" +
              "oauth_verifier=" + verifier;

            
            var Request = (HttpWebRequest)WebRequest.Create(request_url);
            try
            {
                var Response = (HttpWebResponse)Request.GetResponse();
                using (var Reader = new StreamReader(Response.GetResponseStream(), Encoding.GetEncoding(1251)))
                {
                    var twitterToken = new TwitterToken();
                    var outline = Reader.ReadToEnd();
                    char[] delimiterChars = { '&', '=' };
                    var words = outline.Split(delimiterChars);
                    twitterToken.OauthToken = words[1];
                    twitterToken.OauthTokenSecret = words[3];
                    twitterToken.UserId = words[5];
                    twitterToken.UserName = words[7];

                    return twitterToken;
                }
                
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string SendMessage(string oauthToken, string oauthTokenSecret, string title, string description)
        {
            var sender = new TwitterUpdateWithMedia(consumerKey, consumerSecret, oauthToken, oauthTokenSecret);
            var pic = TwitterPicture.CreatePictureFromText(description);

            return sender.UpdateWithMedia(title, pic);
        }
    }
}
