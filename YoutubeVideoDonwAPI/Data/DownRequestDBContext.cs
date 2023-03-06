using Microsoft.EntityFrameworkCore;
using YoutubeVideoDonwAPI.Model;

namespace YoutubeVideoDonwAPI.Data;

public class DownRequestDBContext : DbContext
{
    public DownRequestDBContext()
    {
    }

    public DownRequestDBContext(DbContextOptions<DownRequestDBContext> options) : base(options)
    {
    }
    //DbSet is representation of Table in DB
    public DbSet<DownloadRequest> DownloadRequests { get; set; }
}
