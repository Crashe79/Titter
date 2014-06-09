using System;
using Core.Domain.MySql;

namespace Titter
{
    public class TwitterToken
    {
        public int Id { get; set; }
        public string UserGuid { get; set; }
        public string OauthToken { get; set; }
        public string OauthTokenSecret { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }

        public static MySqlModel.TwitterToken ConvertToken(TwitterToken token)
        {
            return new MySqlModel.TwitterToken
                {
                    Id = token.Id,
                    UserGuid = token.UserGuid,
                    OauthToken = token.OauthToken,
                    OauthTokenSecret = token.OauthTokenSecret,
                    UserId = token.UserId,
                    UserName = token.UserName
                };
        }
    }
}
