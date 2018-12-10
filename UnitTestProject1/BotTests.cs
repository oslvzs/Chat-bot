using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Chat_bot;

namespace BotTests
{
    [TestClass]
    public class BotTests
    {
        [TestMethod]
        public void MusixmatchTestEnglishLang()
        {
            MusixmatchFinder musixmatchFinder = new MusixmatchFinder();
            var songName = "Billie Jean is not my lover";
            IList<Tuple<string, string, string>> mixmatchResult = musixmatchFinder.FindSongByLyrics(songName);
            Tuple<string, string, string> result = new Tuple<string, string, string>("Billie Jean", "Thriller", "Michael Jackson");
            Assert.AreEqual(result.Item1, mixmatchResult[0].Item1);
            Assert.AreEqual(result.Item2, mixmatchResult[0].Item2);
            Assert.AreEqual(result.Item3, mixmatchResult[0].Item3);
        }


        [TestMethod]
        public void MusixmatchTestRussianhLang()
        {
            MusixmatchFinder musixmatchFinder = new MusixmatchFinder();
            var songName = "Ах эти тучи в голубом";
            IList<Tuple<string, string, string>> mixmatchResult = musixmatchFinder.FindSongByLyrics(songName);
            Tuple<string, string, string> result = new Tuple<string, string, string>("Тучи в голубом", "My Life", "Кристина Орбакайте");
            Assert.AreEqual(result.Item1, mixmatchResult[0].Item1);
            Assert.AreEqual(result.Item2, mixmatchResult[0].Item2);
            Assert.AreEqual(result.Item3, mixmatchResult[0].Item3);
        }

        [TestMethod]
        public void YouTubeTestEnglishhLang()
        {
            YoutubeListener youtubeListener = new YoutubeListener();
            var songName = "Michael Jackson - Billie Jean";
            var result = youtubeListener.TryYoutube(songName);        
            Assert.AreEqual("https://www.youtube.com/watch?v=Zi_XLOBDo_Y", result);
        }

        [TestMethod]
        public void YouTubeTestRussianhLang()
        {
            YoutubeListener youtubeListener = new YoutubeListener();
            var songName = "Кристина Орбакайте - Тучи в голубом";
            var result = youtubeListener.TryYoutube(songName);
            Assert.AreEqual("https://www.youtube.com/watch?v=Xh5IP1lMjNQ", result);
        }
    }
}
