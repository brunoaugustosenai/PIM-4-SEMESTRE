namespace Naxapa.Web.Services;

public sealed class DatabaseRuntimeState
{
    public bool IsReady { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkReady()
    {
        IsReady = true;
        ErrorMessage = null;
    }

    public void MarkFailed(string message)
    {
        IsReady = false;
        ErrorMessage = message;
    }
}
