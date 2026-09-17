using System;
using Serilog;

namespace CryptoTrading.Infrastructure.Logging;

public interface ILoggerService
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
    void Fatal(string message, Exception? ex = null);
}

public class SerilogLoggerService : ILoggerService
{
    public void Debug(string message) => Log.Debug(message);
    public void Info(string message) => Log.Information(message);
    public void Warn(string message) => Log.Warning(message);
    public void Error(string message, Exception? ex = null) => Log.Error(ex, message);
    public void Fatal(string message, Exception? ex = null) => Log.Fatal(ex, message);
}
