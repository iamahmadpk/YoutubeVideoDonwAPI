using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using System.IO.Compression;
using YoutubeExplode.Videos.Streams;
using YoutubeVideoDonwAPI.Model;
using YoutubeVideoDonwAPI.Data;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;


namespace YoutubeVideoDonwAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class VideosController : ControllerBase
{
    private readonly YouTubeService _youTubeService;
    private readonly YoutubeClient youtube;
    private readonly DownRequestDBContext _db;
    private string fileName;

    public VideosController(DownRequestDBContext db) => _db = db;
    

    [HttpGet]
    public async Task<IActionResult> Searchs(string query, int max=1)
    {
        var youtube = new YoutubeClient();
        //GetVideosAsync restrict the limit for only Videos
        var videos = await youtube.Search.GetVideosAsync(query, CancellationToken.None);
        var viewModel = new VideoListViewModel
        {
            Videos = videos,
            
        };
        return Ok(viewModel);
    }

    public class VideoListViewModel
    {
        
        public IReadOnlyList<VideoSearchResult> Videos { get; internal set; }
    }
    //-------------//
    
    [HttpGet("download")]
    public async Task<IActionResult> DownloadVideos([FromQuery] string[] videoIds)
    {
        var youTubeClient = new YoutubeClient();
        //creating an instance of MemoryStream
        var memoryStream = new MemoryStream();
        //use true so that ZipArchive behave in update mode 
        //ZipArchive will be created from scratch, overwriting any existing data in the MemoryStream.
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var videoId in videoIds)
            {
                //Videso.GetAsync =>retrieve the metadata associated with a YouTube video
                var video = await youTubeClient.Videos.GetAsync(videoId);
                //GetManifestAsyn =>request the manifest that lists all available streams for a particular video
                var streamInfoSet = await youTubeClient.Videos.Streams.GetManifestAsync(videoId);
                //GetVideoOnlyStreams=> get only video streAMS
                var streamInfo = streamInfoSet.GetVideoOnlyStreams()
                    .OrderByDescending(s => s.VideoQuality)
                    .FirstOrDefault(s => s.Container == Container.Mp4);
                if (streamInfo == null)
                {
                    continue;
                }
                //actual stream represented by the specified metadata
                var stream = await youTubeClient.Videos.Streams.GetAsync(streamInfo);//
                var fileName = $"{video.Title}.{streamInfo.Container}";
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using (var entryStream = entry.Open())
                {
                    await stream.CopyToAsync(entryStream);
                }
            }
        }//sets the position of the memoryStream object back to the beginning of the stream.
         //This is necessary because the stream was written to while creating the ZIP archive and needs to be reset before being returned
        memoryStream.Seek(0, SeekOrigin.Begin);
        //"application/octet-stream", which is a generic MIME type for binary data
        //return File(memoryStream, "application/octet-stream", "videos.zip");
        //-----------///
        // Amazon S3 credentials and configuration.
        var accessKeyId = "AKIAWZGSM6NUROKLTU7D";
        var secretAccessKey = "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";
        var region = RegionEndpoint.USEast1;
        var bucketName = "ytvideodown";
        var s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, region);

        // Upload the ZIP file to Amazon S3.
        var transferUtility = new TransferUtility(s3Client);
        var key = $"{Guid.NewGuid()}.zip";
        await transferUtility.UploadAsync(memoryStream, bucketName, key);

        // Get a pre-signed URL for downloading the file.
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Expires = DateTime.Now.AddDays(1),
            Verb = HttpVerb.GET
        };
        var url = s3Client.GetPreSignedURL(request);
        
     

        using (var context = _db)
        {   
            
            
            var videoDownloads = new List<DownloadRequest>();
            
           // foreach (var videoId in videoIds)
           // {
                //var video = await youTubeClient.Videos.GetAsync(videoId);
                videoDownloads.Add(new DownloadRequest
                {
                    VideoId = "SampleID",
                    VideoTitle = "DownloadedZip",
                    DownloadUrl = url,
             
                });
           // }
            await context.AddRangeAsync(videoDownloads);
            await context.SaveChangesAsync();
        }

        // Return the download URL as a response.
        return Ok(new { url });
    }
}
