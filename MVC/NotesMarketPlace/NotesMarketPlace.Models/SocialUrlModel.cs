using System.ComponentModel.DataAnnotations;

namespace NotesMarketPlace.Models
{
    public class SocialUrlModel
    {
        [DataType(DataType.Url)]
        [UIHint("OpenInNewWindow")]
        public string Facebook { get; set; }

        [DataType(DataType.Url)]
        [UIHint("OpenInNewWindow")]
        public string Twitter { get; set; }

        [DataType(DataType.Url)]
        [UIHint("OpenInNewWindow")]
        public string Linkedin { get; set; }
    }
}
