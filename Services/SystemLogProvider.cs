using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace WebApplicationCentralino.Services
{
    public class SystemLogProvider : ILoggerProvider
    {
        private readonly ISystemLogService _systemLogService;
        private readonly string _categoryName;

        public SystemLogProvider(ISystemLogService systemLogService, string categoryName)
        {
            _systemLogService = systemLogService;
            _categoryName = categoryName;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SystemLogger(_systemLogService, categoryName);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        private class SystemLogger : ILogger
        {
            private readonly ISystemLogService _systemLogService;
            private readonly string _categoryName;

            public SystemLogger(ISystemLogService systemLogService, string categoryName)
            {
                _systemLogService = systemLogService;
                _categoryName = categoryName;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var message = formatter(state, exception);
                var logEntry = new SystemLog
                {
                    Timestamp = DateTime.Now,
                    Level = logLevel.ToString(),
                    Category = _categoryName,
                    Message = message,
                    Exception = exception?.ToString()
                };

                _systemLogService.AddLog(logEntry);
            }
        }
    }
} 