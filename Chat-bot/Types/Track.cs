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
            this.Name = name;
            this.Album = album;
            this.Performer = performer;
        }

        public string Performer { get; set; }
        public string Name { get; set; }
        public string Album { get; set; }

    }
}
