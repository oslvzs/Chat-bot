﻿using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat_bot
{
    public class YoutubeListener
    {
        private readonly string youtubeToken;

        public YoutubeListener(string token)
        {
            this.youtubeToken = token;
            }


        public string TryYoutube(string songName)
        {
            
            StringBuilder officialVideo = new StringBuilder();
            StringBuilder youtubeLink = new StringBuilder();
            List<string> videos = new List<string>();
            Google.Apis.YouTube.v3.Data.SearchListResponse searchListResponse;
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeToken
            });
            // заполняем запрос нужной инфой
            var searchListRequest = youtubeService.Search.List("snippet");
            //officialVideo = officialVideo.Append(songName).Append(" (Official Music Video)"); попробуем пока без этого
            //searchListRequest.Q = officialVideo.ToString();
            searchListRequest.Q = songName;
            searchListRequest.MaxResults = 10;
            //получаем ответ    
            try
            {
                 searchListResponse = searchListRequest.Execute();
            }
            catch(Exception e)
            {
                Console.WriteLine("При попытке соединения с API Youtube что-то пошло не так!");
                Console.WriteLine(e.Message);
                return null;
            }
           
            // парсим его
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        youtubeLink.Append("https://www.youtube.com/watch?v=").Append(searchResult.Id.VideoId.ToString());
                        videos.Add(String.Format("{0}", youtubeLink.ToString()));
                        youtubeLink.Clear();
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
