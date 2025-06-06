using System.Diagnostics;

namespace WebApplicationCentralino.Services
{
    public class SystemLog
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Exception { get; set; }
    }

    public interface ISystemLogService
    {
        Task<List<SystemLog>> GetSystemLogsAsync();
        Task<SystemLog> GetLogByIdAsync(string id);
        Task<byte[]> GetLogFileContentAsync(string id);
        Task WriteLogAsync(string message, string type, string source);
        Task CleanupOldLogsAsync();
        Task ClearAllLogsAsync();
        void AddLog(SystemLog log);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ILogger<SystemLogService> _logger;
        private readonly string _logDirectory;
        private readonly TimeSpan _logRetentionPeriod = TimeSpan.FromDays(3);

        public SystemLogService(ILogger<SystemLogService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _logDirectory = Path.Combine(env.ContentRootPath, "Logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // Write initial log entry
            WriteLogAsync("Sistema avviato", "INFO", "SystemLogService").Wait();

            // Schedule cleanup of old logs
            Task.Run(async () => await CleanupOldLogsAsync());
        }

        public void AddLog(SystemLog log)
        {
            WriteLogAsync(log.Message, log.Level, log.Category).Wait();
        }

        public async Task WriteLogAsync(string message, string type, string source)
        {
            try
            {
                var timestamp = DateTime.Now;
                var logFileName = $"{timestamp:yyyy-MM-dd}.log";
                var logPath = Path.Combine(_logDirectory, logFileName);

                var logEntry = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{type}] [{source}] {message}";

                await File.AppendAllTextAsync(logPath, logEntry + Environment.NewLine);
                
                // Also log to the standard logger
                switch (type.ToUpper())
                {
                    case "ERROR":
                        _logger.LogError("{Message}", message);
                        break;
                    case "WARNING":
                        _logger.LogWarning("{Message}", message);
                        break;
                    case "INFO":
                        _logger.LogInformation("{Message}", message);
                        break;
                    default:
                        _logger.LogDebug("{Message}", message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la scrittura del log");
            }
        }

        public async Task CleanupOldLogsAsync()
        {
            try
            {
                var cutoffDate = DateTime.Now.Subtract(_logRetentionPeriod);
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        fileInfo.Delete();
                        await WriteLogAsync($"File di log eliminato: {fileInfo.Name}", "INFO", "SystemLogService");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia dei vecchi log");
            }
        }

        public async Task<List<SystemLog>> GetSystemLogsAsync()
        {
            var logs = new List<SystemLog>();
            var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                                  .OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var file in logFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var logContent = await File.ReadAllLinesAsync(file);
                    
                    foreach (var line in logContent)
                    {
                        if (TryParseLogLine(line, out var log))
                        {
                            logs.Add(log);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Errore nella lettura del file di log {file}");
                }
            }

            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }

        public async Task<SystemLog> GetLogByIdAsync(string id)
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            foreach (var file in logFiles)
            {
                var logContent = await File.ReadAllLinesAsync(file);
                foreach (var line in logContent)
                {
                    if (TryParseLogLine(line, out var log) && log.Id == id)
                    {
                        return log;
                    }
                }
            }
            return null;
        }

        public async Task<byte[]> GetLogFileContentAsync(string id)
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            foreach (var file in logFiles)
            {
                var logContent = await File.ReadAllLinesAsync(file);
                foreach (var line in logContent)
                {
                    if (TryParseLogLine(line, out var log) && log.Id == id)
                    {
                        return System.Text.Encoding.UTF8.GetBytes(line);
                    }
                }
            }
            return null;
        }

        public async Task ClearAllLogsAsync()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                foreach (var file in logFiles)
                {
                    File.Delete(file);
                }
                await WriteLogAsync("Tutti i log sono stati cancellati", "INFO", "SystemLogService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione dei log");
                throw;
            }
        }

        private bool TryParseLogLine(string line, out SystemLog log)
        {
            log = null;
            try
            {
                // Expected format: [2024-03-21 10:30:45] [INFO] [Source] Message
                var parts = line.Split(new[] { ']' }, 4);
                if (parts.Length != 4) return false;

                var timestampStr = parts[0].TrimStart('[');
                var type = parts[1].TrimStart('[').Trim();
                var source = parts[2].TrimStart('[').Trim();
                var message = parts[3].Trim();

                if (DateTime.TryParse(timestampStr, out var timestamp))
                {
                    log = new SystemLog
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = timestamp,
                        Type = type,
                        Source = source,
                        Message = message
                    };
                    return true;
                }
            }
            catch
            {
                // Parsing failed
            }
            return false;
        }
    }
} 