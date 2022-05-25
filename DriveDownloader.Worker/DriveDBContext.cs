using System.Reflection;
using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveDownloader.Worker;

public class DriveDBContext: DbContext
{
    public DbSet<GoogleDriveFile> Files { get; set; }
    public DbSet<Status> Status { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@"Filename=/data/googledrive.db", options =>
        {
            options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoogleDriveFile>(
            entity => {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullPath);
                entity.Property(e => e.TotalBytes);
                entity.Property(e => e.BytesDownloaded);
                entity.Property(e => e.State).HasConversion<int>();
            }
        );

        modelBuilder.Entity<Status>(
            entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DownloadThreads);
                entity.Property(e => e.IsRunning);
                entity.Property(e => e.AppName);
                entity.Property(e => e.ClientId);
                entity.Property(e => e.ClientSecret);
                entity.Property(e => e.LocalBaseFolder);
                entity.Property(e => e.RemoteBaseFolder);
                
            });
        
        base.OnModelCreating(modelBuilder);
    }
    
    
}