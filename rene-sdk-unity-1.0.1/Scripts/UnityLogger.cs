using UnityEngine;
using ILogger = ReneSdk.Rene.Sdk.LoggingService.ILogger;

public class UnityLogger : ILogger
{
    public void Log(string message)
    {
        Debug.LogWarning(message);
    }
}
