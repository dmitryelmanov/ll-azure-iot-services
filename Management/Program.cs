using Common;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Action = Management.Action;

const string HUB_CONNECTION_STRING = "<IoT_Hub_registryReadWrite_Connection_String>";

ConsoleWriter.WriteLine("Start back-end application", ConsoleColor.Cyan);
ConsoleWriter.Write("Creating registry manager... ", ConsoleColor.Cyan);
using var registry = RegistryManager.CreateFromConnectionString(HUB_CONNECTION_STRING);
ConsoleWriter.WriteLine("OK", ConsoleColor.Green);

using var cts = new CancellationTokenSource();
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
        case Action.ListDevices:
            await ListDevicesAsync();
            break;
        case Action.CreateDevice:
            await CreateDeviceAsync();
            break;
        case Action.RemoveDevice:
            await RemoveDeviceAsync();
            break;
    }
},
new Loop.Options
{
    IntervalMs = 1 * 1000,
    CancellationTokenSource = cts,
});

ConsoleWriter.WriteLine("Exit back-end application", ConsoleColor.Cyan);


async Task ListDevicesAsync()
{
    ConsoleWriter.Write("Getting all devices... ", ConsoleColor.Cyan);
    var twins = new List<Twin>();
    var query = registry!.CreateQuery("select * from devices", 10);
    while (query.HasMoreResults)
    {
        var page = await query.GetNextAsTwinAsync();
        twins.AddRange(page);
    }
    ConsoleWriter.WriteLine("OK", ConsoleColor.Green);

    foreach (var twin in twins)
    {
        ConsoleWriter.WriteLine(twin.ToJson(Newtonsoft.Json.Formatting.Indented), ConsoleColor.Blue);
    }
}

async Task CreateDeviceAsync()
{
    ConsoleWriter.WriteLine("Create new device", ConsoleColor.Cyan);
    ConsoleWriter.WriteLine("Ented device Id", ConsoleColor.Green);
    var id = Console.ReadLine();
    if (string.IsNullOrEmpty(id)) return;

    var device = new Device(id)
    {
        Authentication = new AuthenticationMechanism
        {
            Type = AuthenticationType.Sas,
        },
    };
    var result = await registry!.AddDeviceAsync(device);
    ConsoleWriter.WriteLine($"New device {result.Id} created", ConsoleColor.Green);
}

async Task RemoveDeviceAsync()
{
    ConsoleWriter.WriteLine("Remove device", ConsoleColor.Cyan);
    ConsoleWriter.WriteLine("Ented device Id", ConsoleColor.Green);
    var id = Console.ReadLine();
    if (string.IsNullOrEmpty(id)) return;

    await registry!.RemoveDeviceAsync(id);
    ConsoleWriter.WriteLine($"Device {id} removed", ConsoleColor.Green);
}

