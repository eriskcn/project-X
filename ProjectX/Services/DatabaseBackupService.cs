using System.Diagnostics;

namespace ProjectX.Services;

public class DatabaseBackupService(ILogger<DatabaseBackupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database Backup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(2);
                var delay = nextRun - now;

                logger.LogInformation($"Next backup scheduled at {nextRun}.");

                await Task.Delay(delay, stoppingToken);

                // Perform the database backup
                await BackupDatabaseAsync();
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Database Backup Service is stopping.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while performing the database backup.");
            }
        }
    }

    private async Task BackupDatabaseAsync()
    {
        logger.LogInformation("Starting database backup...");

        const string databaseName = "project_x";
        var backupFilePath = $"/var/opt/mssql/backups/{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        DotNetEnv.Env.Load();

        var password = Environment.GetEnvironmentVariable("SA_PASSWORD");
        if (string.IsNullOrEmpty(password))
        {
            logger.LogError("Database password is not set in the environment variables.");
            return;
        }

        var tSqlCommand = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{backupFilePath}' WITH INIT";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sqlcmd",
                Arguments = $"-S localhost -U sa -P {password} -Q \"{tSqlCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            logger.LogInformation("Database backup completed successfully.");
            logger.LogInformation(output);
        }
        else
        {
            logger.LogError($"Database backup failed: {error}");
        }
    }
}