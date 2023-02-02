using System.Reflection;
using log4net;

namespace Obj2Tiles.Common;

public interface ILogger
{
    void Debug(string message);
    void Info(string message, string component = "");
    void Error(string message, Exception? ex = null);
}

public class Logger : ILogger
{
    private readonly ILog _logger;

    public Logger()
    {
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    }

    public void Debug(string message)
    {
        _logger?.Debug(message);
    }

    public void Info(string message, string component = "")
    {
        _logger?.Info(component + ": " + message);
    }

    public void Error(string message, Exception? ex = null)
    {
        _logger?.Error(message, ex?.InnerException);
    }
}