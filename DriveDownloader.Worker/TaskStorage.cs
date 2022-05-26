namespace DriveDownloader.Worker;

internal static class TaskStorage
{
    private static List<Task> Tasks { get; set; } = new();
    private static readonly CancellationTokenSource TokenSource = new();
    private static CancellationToken Token => TokenSource.Token;

    public static void RunAndStore(Func<CancellationToken,Task> action)
    {
        Tasks.Add(action(Token));
    }

    public static void CancelTasks()
    {
        TokenSource.Cancel();
    }

    public static void WaitAll()
    {
        Task.WaitAll(Tasks.ToArray(), Token);
    }

}