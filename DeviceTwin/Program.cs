using Common;
using DeviceTwin;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Action = DeviceTwin.Action;

const string HUB_CONNECTION_STRING = "<IoT_Hub_service_Connection_String>";
const string DEVICE_ID = "<Device_Id>";
var twin = default(Twin);

ConsoleWriter.WriteLine("Starting back-end application", ConsoleColor.Cyan);
ConsoleWriter.Write("Creating registry manager... ", ConsoleColor.Cyan);
using var registry = RegistryManager.CreateFromConnectionString(HUB_CONNECTION_STRING);
ConsoleWriter.WriteLine("OK", ConsoleColor.Green);

await GetAndPrintDeviceTwinAsync();

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
        case Action.UpdateProperty:
            await UpdatePropertyAsync();
            break;
        case Action.UpdateTag:
            await UpdateTagAsync();
            break;
        case Action.ReadTwin:
            await GetAndPrintDeviceTwinAsync();
            break;
    }
},
new Loop.Options { IntervalMs = 1 * 1000 });


async Task GetAndPrintDeviceTwinAsync()
{
    ConsoleWriter.Write("Getting device twin... ", ConsoleColor.Cyan);
    twin = await registry!.GetTwinAsync(DEVICE_ID);
    ConsoleWriter.WriteLine("OK", ConsoleColor.Green);
    ConsoleWriter.WriteLine("Twin:", ConsoleColor.Cyan);
    ConsoleWriter.WriteLine(twin.ToJson(Formatting.Indented), ConsoleColor.Blue);
}

async Task UpdatePropertyAsync()
{
    ConsoleWriter.WriteLine("Enter desired property Name", ConsoleColor.Green);
    var name = Console.ReadLine();
    if (string.IsNullOrEmpty(name)) return;

    ConsoleWriter.WriteLine($"Enter Value for {name}", ConsoleColor.Green);
    var value = Console.ReadLine();
    if (string.IsNullOrEmpty(value)) return;

    ConsoleWriter.WriteLine($"Select the Type for {name}", ConsoleColor.Green);
    ConsoleWriter.WriteLine($"\t{PropertyType.String}: {(int)PropertyType.String}", ConsoleColor.Magenta);
    ConsoleWriter.WriteLine($"\t{PropertyType.Int}: {(int)PropertyType.Int}", ConsoleColor.Magenta);
    ConsoleWriter.WriteLine($"\t{PropertyType.Double}: {(int)PropertyType.Double}", ConsoleColor.Magenta);
    var typeStr = Console.ReadLine();
    if (string.IsNullOrEmpty(typeStr)) return;
    var type = Enum.Parse<PropertyType>(typeStr);

    var twinPatch = new Twin();
    twinPatch.Properties.Desired[name] = type switch
    {
        PropertyType.Double => Convert.ToDouble(value),
        PropertyType.Int => Convert.ToInt32(value),
        _ => value,
    };
    await registry.UpdateTwinAsync(DEVICE_ID, twinPatch, twin!.ETag);
    await GetAndPrintDeviceTwinAsync();
}

async Task UpdateTagAsync()
{
    ConsoleWriter.WriteLine("Enter tag Name", ConsoleColor.Green);
    var name = Console.ReadLine();
    if (string.IsNullOrEmpty(name)) return;

    ConsoleWriter.WriteLine($"Enter Value for {name}", ConsoleColor.Green);
    var value = Console.ReadLine();
    if (string.IsNullOrEmpty(value)) return;

    var twinPatch = new Twin();
    twinPatch.Tags[name] = value;
    await registry!.UpdateTwinAsync(DEVICE_ID, twinPatch, twin!.ETag);
    await GetAndPrintDeviceTwinAsync();
}
