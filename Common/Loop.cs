namespace Common;

public static class Loop
{
    public delegate Task ExecuteMethod();

    public static async Task ExecuteAsync(ExecuteMethod method, Options options)
    {
        var cts = options.CancellationTokenSource ?? new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        Console.WriteLine("Press Control+C to quit");

        try
        {
            while (!cts.IsCancellationRequested)
            {
                await method();
                await Task.Delay(options.IntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (options.CancellationTokenSource is null)
            {
                cts.Dispose();
            }
        }
    }

    public class Options
    {
        public int IntervalMs { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}
