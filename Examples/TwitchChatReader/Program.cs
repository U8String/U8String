using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using U8.IO;

using var cts = new CancellationTokenSource();
using var sigint = PosixSignalRegistration.Create(
    PosixSignal.SIGINT, ctx =>
{
    ctx.Cancel = true;
    cts.Cancel();
});

var channels = args.Select(U8String.Create).ToArray();
if (channels is [])
{
    U8Console.WriteLine("Usage: <channel1> <channel2>..."u8);
    return;
}

U8Console.WriteLine($"Connecting to {U8String.Join(", "u8, channels)}...");
using var socket = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp);

var address = await Dns.GetHostAddressesAsync("irc.chat.twitch.tv");
await socket.ConnectAsync(address, 6667, cts.Token);
await socket.SendAsync(u8("PASS SCHMOOPIIE\r\n"), cts.Token);
await socket.SendAsync(u8("NICK justinfan54970\r\n"), cts.Token);
await socket.SendAsync(u8("USER justinfan54970 8 * :justinfan54970\r\n"), cts.Token);
foreach (var channel in channels)
{
    await socket.SendAsync($"JOIN #{channel}\r\n", ct: cts.Token);
}
U8Console.WriteLine("Connected! To exit, press Ctrl+C."u8);

try
{
    await foreach (var line in socket
        .ReadU8Lines(disposeSource: false)
        .WithCancellation(cts.Token))
    {
        var message = Message.Parse(line);
        if (message is null) continue;
        if (message.Command == "PING"u8)
        {
            await socket.SendAsync(u8("PONG :tmi.twitch.tv\r\n"));
        }
        else U8Console.WriteLine($"#{message.Channel} {message.Nickname}: {message.Body}");
    }
}
catch (OperationCanceledException) { }

foreach (var channel in channels)
{
    await socket.SendAsync($"PART #{channel}\r\n");
}
await socket.DisconnectAsync(false);
U8Console.WriteLine("Goodbye!"u8);
