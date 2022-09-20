using Azure.Messaging.EventHubs.Consumer;
using Common;

const string HUB_CONNECTION_STRING = "<IoT_Hub_Built_In_Endpoints_Event_Hub_compatible_endpoint>";

ConsoleWriter.WriteLine("Start back-end application", ConsoleColor.Cyan);
ConsoleWriter.Write("Creating Event Hubs consumer client... ", ConsoleColor.Cyan);
await using var client = new EventHubConsumerClient(
    EventHubConsumerClient.DefaultConsumerGroupName,
    HUB_CONNECTION_STRING);
ConsoleWriter.WriteLine("OK", ConsoleColor.Green);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};
Console.WriteLine("Press Control+C to quit");

await foreach (var partitionEvent in client.ReadEventsAsync(new ReadEventOptions 
    {
        MaximumWaitTime = TimeSpan.FromMilliseconds(500),
    }))
{
    if (partitionEvent.Data != null)
    {
        ConsoleWriter.WriteLine("New data received", ConsoleColor.Yellow);
        ConsoleWriter.WriteLine($"\tMessageId: {partitionEvent.Data.MessageId}", ConsoleColor.Blue);
        ConsoleWriter.WriteLine($"\tPartitionKey: {partitionEvent.Data.PartitionKey}", ConsoleColor.Blue);
        ConsoleWriter.WriteLine($"\tContentType: {partitionEvent.Data.ContentType}", ConsoleColor.Blue);
        ConsoleWriter.WriteLine("\tProperties:", ConsoleColor.Blue);
        foreach (var prop in partitionEvent.Data.Properties)
        {
            ConsoleWriter.WriteLine($"\t\t{prop.Key}: {prop.Value}", ConsoleColor.Blue);
        }
        ConsoleWriter.WriteLine("\tSystemProperties:", ConsoleColor.Blue);
        foreach (var prop in partitionEvent.Data.SystemProperties)
        {
            ConsoleWriter.WriteLine($"\t\t{prop.Key}: {prop.Value}", ConsoleColor.Blue);
        }
        ConsoleWriter.WriteLine($"\tEventBody: {partitionEvent.Data.EventBody}", ConsoleColor.Blue);
    }

    if (cts.IsCancellationRequested) break;
}

ConsoleWriter.WriteLine("Exit back-end application", ConsoleColor.Cyan);
