namespace DriveDownloader.Worker.Models;

public class Errors
{
    public DateTime DateTime { get; set; }
    public string Stacktrace { get; set; }
    public string Message { get; set; }
    public int? Id { get; set; }
    
    public string Function { get; set; }
    
    public string Details { get; set; }
}