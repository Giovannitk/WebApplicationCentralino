using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace WebApplicationCentralino.Services
{
    public interface ILoginAttemptService
    {
        bool IsUserLocked(string email);
        void RecordFailedAttempt(string email);
        void ResetAttempts(string email);
        int GetRemainingAttempts(string email);
    }

    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginAttemptService> _logger;

        public LoginAttemptService(
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<LoginAttemptService> logger)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsUserLocked(string email)
        {
            var lockoutKey = $"lockout_{email}";
            return _cache.TryGetValue(lockoutKey, out _);
        }

        public void RecordFailedAttempt(string email)
        {
            var attemptsKey = $"attempts_{email}";
            var currentAttempts = _cache.GetOrCreate(attemptsKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return 0;
            });

            currentAttempts++;
            _cache.Set(attemptsKey, currentAttempts, TimeSpan.FromMinutes(30));

            var maxAttempts = _configuration.GetValue<int>("Authentication:MaxLoginAttempts", 5);
            var lockoutDuration = _configuration.GetValue<int>("Authentication:LockoutDuration", 10);

            if (currentAttempts >= maxAttempts)
            {
                var lockoutKey = $"lockout_{email}";
                _cache.Set(lockoutKey, true, TimeSpan.FromMinutes(lockoutDuration));
                _logger.LogWarning("Account {Email} locked for {Duration} minutes due to too many failed attempts", 
                    email, lockoutDuration);
            }
        }

        public void ResetAttempts(string email)
        {
            var attemptsKey = $"attempts_{email}";
            var lockoutKey = $"lockout_{email}";
            _cache.Remove(attemptsKey);
            _cache.Remove(lockoutKey);
        }

        public int GetRemainingAttempts(string email)
        {
            var maxAttempts = _configuration.GetValue<int>("Authentication:MaxLoginAttempts", 5);
            var attemptsKey = $"attempts_{email}";
            
            if (_cache.TryGetValue(attemptsKey, out int currentAttempts))
            {
                return Math.Max(0, maxAttempts - currentAttempts);
            }
            
            return maxAttempts;
        }
    }
} 