using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace YoutubeVideoDonwAPI.Model
{
    public class DownloadRequest
    {
        [Key]
        public int Id { get; set; }
        public string VideoId { get; set; }
        public string VideoTitle { get; set; }
        public string DownloadUrl { get; set; }
        //public string Status { get; set; }
    }
}
