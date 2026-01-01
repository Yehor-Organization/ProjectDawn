using System.Data.Common;

namespace ProjectDawnApi;

public static class DatabaseMigrationHelper
{
    public static bool IsTransientDbError(Exception ex)
    {
        if (ex is DbException)
            return true;

        if (ex.InnerException is DbException)
            return true;

        var message = ex.Message ?? string.Empty;

        return message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase);
    }
}