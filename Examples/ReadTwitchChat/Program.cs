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

if (args is not [var chan] || chan is [])
{
    U8Console.WriteLine("Usage: <channel>"u8);
    return;
}
U8Console.WriteLine($"Connecting to Twitch channel {chan}...");

using var sock = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp);

var addr = await Dns.GetHostAddressesAsync("irc.chat.twitch.tv");

await sock.ConnectAsync(addr, 6667, cts.Token);
await sock.SendAsync(u8("PASS SCHMOOPIIE\r\n"), cts.Token);
await sock.SendAsync(u8("NICK justinfan54970\r\n"), cts.Token);
await sock.SendAsync(u8("USER justinfan54970 8 * :justinfan54970\r\n"), cts.Token);
await sock.SendAsync(u8($"JOIN #{chan}\r\n"), cts.Token);
U8Console.WriteLine("Connected! To exit, press Ctrl+C."u8);

try
{
    await foreach (var line in sock
        .AsU8Reader()
        .Lines
        .WithCancellation(cts.Token))
    {
        var msg = Message.Parse(line);
        if (msg is null) continue;
        if (msg.Command == "PING"u8)
        {
            await sock.SendAsync(u8("PONG :tmi.twitch.tv\r\n"));
        }
        else PrintMessage(msg);
    }
}
catch (OperationCanceledException) { }

await sock.SendAsync(u8($"PART #{chan}\r\n"));
await sock.DisconnectAsync(false);

U8Console.WriteLine("Goodbye!"u8);

static void PrintMessage(Message msg) =>
    U8Console.WriteLine($"{msg.Nickname}: {msg.Body}");