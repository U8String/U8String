// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using U8.InteropServices;

var text = u8($"Today is {DateTime.Now:yyyy-MM-dd}.");

Interop.Print(text, text.Length);
Interop.PrintNullTerminated(text);

var rcountCsharp = text.RuneCount;
var rcountRust = (int)Interop.CountRunes(text, text.Length);
if (rcountCsharp != rcountRust)
    throw new InvalidOperationException("Rune count mismatch.");

var fromRust = Interop.GetString();
U8Console.WriteLine(fromRust);

static unsafe partial class Interop
{
    [SuppressGCTransition]
    [LibraryImport("rust", EntryPoint = "print")]
    public static partial void Print(U8String text, nint length);

    [SuppressGCTransition]
    [LibraryImport("rust", EntryPoint = "print_null_terminated")]
    public static partial void PrintNullTerminated(
        [MarshalUsing(typeof(U8Marshalling.LikelyNullTerminated))] U8String text);

    [SuppressGCTransition]
    [LibraryImport("rust", EntryPoint = "count_runes")]
    public static partial nuint CountRunes(U8String text, nint length);

    [SuppressGCTransition]
    [LibraryImport("rust", EntryPoint = "get_str")]
    public static partial U8String GetString();
}