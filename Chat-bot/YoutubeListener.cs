using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot
{
    class YoutubeListener
    {
        public  string TryYoutube(string songName)
        {
            StringBuilder officialVideo = new StringBuilder();
            StringBuilder youtubeLink = new StringBuilder();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyASNqKt_dBol98tLlQDxQuZbbUPP3S4zF4"
            });
            // заполняем запрос нужной инфой
            var searchListRequest = youtubeService.Search.List("snippet");
            officialVideo = officialVideo.Append(songName).Append(" (Official Music Video)");
            searchListRequest.Q = officialVideo.ToString();
            searchListRequest.MaxResults = 10;
            //получаем ответ
            var searchListResponse = searchListRequest.Execute();
            List<string> videos = new List<string>();
            // парсим его
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        youtubeLink.Append("https://www.youtube.com/watch?v=").Append(searchResult.Id.VideoId.ToString());
                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, youtubeLink.ToString()));
                        break;
                    case "youtube#channel":
                        continue;
                    case "youtube#playlist":
                        continue;
                }                              
            }
            // на месте 8)
                return videos[0];
        }
    }
}
