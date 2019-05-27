using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot.Types
{
     public class Track
    {
        public Track(string name, string album, string performer)
        {
            this.name = name;
            this.album = album;
            this.performer = performer;
        }

        public string performer { get; set; }
        public string name { get; set; }
        public string album { get; set; }

    }
}
