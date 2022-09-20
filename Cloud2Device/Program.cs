using System.Text;
using Common;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Action = Cloud2Device.Action;

const string HUB_CONNECTION_STRING = "<IoT_Hub_service_Connection_String>";
const string DEVICE_ID = "<Device_Id>";

ConsoleWriter.WriteLine("Starting back-end application", ConsoleColor.Cyan);
ConsoleWriter.Write("Creating service client... ", ConsoleColor.Cyan);
using var client = ServiceClient.CreateFromConnectionString(
    HUB_CONNECTION_STRING,
    TransportType.Amqp,
    new ServiceClientOptions
    {
        SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
    });
ConsoleWriter.WriteLine("OK", ConsoleColor.Green);

using var cts = new CancellationTokenSource();
var feedbackTask = ReceiveFeedbackAsync();
await Loop.ExecuteAsync(async () =>
{
    ConsoleWriter.WriteLine("What do you whant to do?", ConsoleColor.Green);
    foreach (var item in Enum.GetValues<Action>())
    {
        ConsoleWriter.WriteLine($"\t{item}: {(int)item}", ConsoleColor.Magenta);
    }
    var action = Console.ReadLine();
    if (string.IsNullOrEmpty(action)) return;

    switch (Enum.Parse<Action>(action))
    {
        case Action.SendC2DMessage:
            await SendC2DMessageAsync();
            break;
        case Action.InvokeDirectMethod:
            await InvokeMethodAsync();
            break;
    }
},
new Loop.Options
{
    IntervalMs = 1 * 1000,
    CancellationTokenSource = cts,
});

ConsoleWriter.WriteLine("Exit back-end application", ConsoleColor.Cyan);
await feedbackTask;

async Task SendC2DMessageAsync()
{
    ConsoleWriter.WriteLine("Enter a message", ConsoleColor.Green);
    var msg = Console.ReadLine();
    if (string.IsNullOrEmpty(msg)) return;

    using var message = new Message(Encoding.UTF8.GetBytes(msg))
    {
        Ack = DeliveryAcknowledgement.Full,
    };
    await client!.SendAsync(DEVICE_ID, message, TimeSpan.FromSeconds(10));
    ConsoleWriter.WriteLine($"Message with Id {message.MessageId} has sent", ConsoleColor.Cyan);
}

async Task InvokeMethodAsync()
{
    ConsoleWriter.WriteLine("Enter a method's name", ConsoleColor.Green);
    var methodName = Console.ReadLine();
    if (string.IsNullOrEmpty(methodName)) return;

    ConsoleWriter.WriteLine("[optional] Enter payload JSON", ConsoleColor.Green);
    var payload = Console.ReadLine();

    var method = new CloudToDeviceMethod(methodName)
    {
        ResponseTimeout = TimeSpan.FromSeconds(10),
    };
    if (!string.IsNullOrWhiteSpace(payload))
    {
        method.SetPayloadJson(payload);
    }

    ConsoleWriter.WriteLine($"Invoke {methodName} method", ConsoleColor.Cyan);
    var result = await client!.InvokeDeviceMethodAsync(DEVICE_ID, method, cts!.Token);

    ConsoleWriter.WriteLine($"Method {methodName} was invoked on device {DEVICE_ID}", ConsoleColor.Blue);
    ConsoleWriter.WriteLine($"Result {result.Status}. Payload:", ConsoleColor.Blue);
    ConsoleWriter.WriteLine(result.GetPayloadAsJson(), ConsoleColor.Magenta);
}

async Task ReceiveFeedbackAsync()
{
    var feedbackReceiver = client!.GetFeedbackReceiver();
    var cancellationToken = cts.Token;

    await Loop.ExecuteAsync(async () =>
    {
        var feedback = await feedbackReceiver.ReceiveAsync(cancellationToken);
        if (feedback != null)
        {
            ConsoleWriter.WriteLine("New Feedback received:", ConsoleColor.Yellow);
            ConsoleWriter.WriteLine($"\tEnqueue Time: {feedback.EnqueuedTime}", ConsoleColor.Magenta);
            ConsoleWriter.WriteLine($"\tNumber of messages in the batch: {feedback.Records.Count()}", ConsoleColor.Magenta);

            foreach (var feedbackRecord in feedback.Records)
            {
                ConsoleWriter.WriteLine($"\tDevice {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}", ConsoleColor.Blue);
            }

            await feedbackReceiver.CompleteAsync(feedback, cancellationToken);
        }
    },
    new Loop.Options
    {
        IntervalMs = 1 * 1000,
        CancellationTokenSource = cts,
    });
}
