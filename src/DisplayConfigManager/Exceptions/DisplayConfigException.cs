namespace DisplayConfigManager.Exceptions;

internal sealed class DisplayConfigException : Exception
{
    public int Win32ErrorCode { get; }

    public DisplayConfigException(string message, int win32ErrorCode = 0)
        : base(message)
    {
        Win32ErrorCode = win32ErrorCode;
    }
}
