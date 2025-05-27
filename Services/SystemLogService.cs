using System.Diagnostics;

namespace WebApplicationCentralino.Services
{
    public class SystemLog
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public interface ISystemLogService
    {
        Task<List<SystemLog>> GetSystemLogsAsync();
        Task<SystemLog> GetLogByIdAsync(string id);
        Task<byte[]> GetLogFileContentAsync(string id);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ILogger<SystemLogService> _logger;
        private readonly string _logDirectory;

        public SystemLogService(ILogger<SystemLogService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _logDirectory = Path.Combine(env.ContentRootPath, "Logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task<List<SystemLog>> GetSystemLogsAsync()
        {
            var logs = new List<SystemLog>();
            var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                                  .OrderByDescending(f => File.GetLastWriteTime(f))
                                  .Take(100); // Limita a 100 log più recenti

            foreach (var file in logFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var logContent = await File.ReadAllTextAsync(file);
                    
                    logs.Add(new SystemLog
                    {
                        Id = fileName,
                        Timestamp = File.GetLastWriteTime(file),
                        Type = GetLogType(fileName),
                        Message = logContent
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Errore nella lettura del file di log {file}");
                }
            }

            return logs;
        }

        public async Task<SystemLog> GetLogByIdAsync(string id)
        {
            var logPath = Path.Combine(_logDirectory, $"{id}.log");
            if (!File.Exists(logPath))
            {
                return null;
            }

            var logContent = await File.ReadAllTextAsync(logPath);
            return new SystemLog
            {
                Id = id,
                Timestamp = File.GetLastWriteTime(logPath),
                Type = GetLogType(id),
                Message = logContent
            };
        }

        public async Task<byte[]> GetLogFileContentAsync(string id)
        {
            var logPath = Path.Combine(_logDirectory, $"{id}.log");
            if (!File.Exists(logPath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(logPath);
        }

        private string GetLogType(string fileName)
        {
            if (fileName.Contains("error", StringComparison.OrdinalIgnoreCase))
                return "Error";
            if (fileName.Contains("warning", StringComparison.OrdinalIgnoreCase))
                return "Warning";
            if (fileName.Contains("info", StringComparison.OrdinalIgnoreCase))
                return "Info";
            return "Debug";
        }
    }
} 