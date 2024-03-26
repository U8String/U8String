record Message(U8String Command, U8String? Nickname, U8String? Channel, U8String? Body)
{
    // The parsing logic below does not allocate, besides the Message object itself.
    public static Message? Parse(U8String line)
    {
        // Remove line terminator if any
        line = line.StripSuffix("\r\n"u8);
        if (line.IsEmpty)
        {
            return null;
        }

        // Skip tags
        if (line.StartsWith('@'))
            line = line[1..].SplitFirst(' ').Remainder;

        // Parse nickname or host
        var nickname = (U8String?)null;
        if (line.StartsWith(':'))
        {
            (var hostmask, line) = line[1..].SplitFirst(' ');
            nickname = hostmask.SplitFirst('!').Segment;
        }

        // Parse command
        (var command, line) = line.SplitFirst(' ');
        if (command.IsEmpty)
        {
            throw new FormatException("Command not found in the message.");
        }

        // Parse channel
        var channel = (U8String?)null;
        if (line.StartsWith('#'))
        {
            (var chan, line) = line[1..].SplitFirst(' ');
            channel = chan;
        }

        // Parse body
        var body = !line.IsEmpty ? line.StripPrefix(':') : (U8String?)null;

        return new(command, nickname, channel, body);
    }
}