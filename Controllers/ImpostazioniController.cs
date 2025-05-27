using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApplicationCentralino.Services;
using System.Diagnostics;
using System.Threading;

namespace WebApplicationCentralino.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImpostazioniController : Controller
    {
        private readonly ISystemLogService _systemLogService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImpostazioniController> _logger;

        public ImpostazioniController(
            ISystemLogService systemLogService,
            IConfiguration configuration,
            ILogger<ImpostazioniController> logger)
        {
            _systemLogService = systemLogService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Recupera le impostazioni attuali
            ViewBag.ExpireTimeSpan = _configuration.GetValue<TimeSpan>("Authentication:ExpireTimeSpan", TimeSpan.FromDays(7));
            ViewBag.MaxLoginAttempts = _configuration.GetValue<int>("Authentication:MaxLoginAttempts", 5);
            ViewBag.LockoutDuration = _configuration.GetValue<int>("Authentication:LockoutDuration", 10);
            
            // Impostazioni interfaccia
            ViewBag.CurrentTheme = _configuration.GetValue<string>("Interface:Theme", "light");
            ViewBag.CurrentLanguage = _configuration.GetValue<string>("Interface:Language", "it");
            ViewBag.CurrentDateFormat = _configuration.GetValue<string>("Interface:DateFormat", "dd/MM/yyyy");
            ViewBag.CurrentTimezone = _configuration.GetValue<string>("Interface:Timezone", "Europe/Rome");

            // Recupera i log di sistema
            ViewBag.SystemLogs = await _systemLogService.GetSystemLogsAsync();

            // Recupera le risorse di sistema
            ViewBag.CpuUsage = GetCpuUsage();
            ViewBag.RamUsage = GetRamUsage();
            ViewBag.DiskUsage = GetDiskUsage();

            return View();
        }

        [HttpPost]
        public IActionResult UpdateExpireTime(int value, string unit)
        {
            try
            {
                TimeSpan newExpireTime;
                switch (unit.ToLower())
                {
                    case "minutes":
                        newExpireTime = TimeSpan.FromMinutes(value);
                        break;
                    case "hours":
                        newExpireTime = TimeSpan.FromHours(value);
                        break;
                    case "days":
                        newExpireTime = TimeSpan.FromDays(value);
                        break;
                    default:
                        throw new ArgumentException("Unità di tempo non valida");
                }

                // Aggiorna la configurazione
                _configuration["Authentication:ExpireTimeSpan"] = newExpireTime.ToString();
                
                TempData["SuccessMessage"] = "Durata token aggiornata con successo";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento della durata del token");
                TempData["ErrorMessage"] = "Si è verificato un errore durante l'aggiornamento";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateLoginAttempts(int maxAttempts, int lockoutDuration)
        {
            try
            {
                _configuration["Authentication:MaxLoginAttempts"] = maxAttempts.ToString();
                _configuration["Authentication:LockoutDuration"] = lockoutDuration.ToString();
                
                TempData["SuccessMessage"] = "Impostazioni di sicurezza aggiornate con successo";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento delle impostazioni di sicurezza");
                TempData["ErrorMessage"] = "Si è verificato un errore durante l'aggiornamento";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateInterfaceSettings(string theme, string language, string dateFormat, string timezone)
        {
            try
            {
                _configuration["Interface:Theme"] = theme;
                _configuration["Interface:Language"] = language;
                _configuration["Interface:DateFormat"] = dateFormat;
                _configuration["Interface:Timezone"] = timezone;
                
                TempData["SuccessMessage"] = "Impostazioni interfaccia aggiornate con successo";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento delle impostazioni interfaccia");
                TempData["ErrorMessage"] = "Si è verificato un errore durante l'aggiornamento";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadLog(string id)
        {
            try
            {
                var logContent = await _systemLogService.GetLogFileContentAsync(id);
                if (logContent == null)
                {
                    return NotFound();
                }

                return File(logContent, "text/plain", $"{id}.log");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il download del log {id}");
                return StatusCode(500, "Errore durante il download del log");
            }
        }

        [HttpGet]
        public IActionResult GetSystemResources()
        {
            return Json(new
            {
                cpuUsage = GetCpuUsage(),
                ramUsage = GetRamUsage(),
                diskUsage = GetDiskUsage()
            });
        }

        private double GetCpuUsage()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                Thread.Sleep(1000);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;
                var cpuUsageTotal = cpuUsedMs / totalMsPassed * 100;

                return Math.Round(cpuUsageTotal, 2);
            }
            catch
            {
                return 0;
            }
        }

        private double GetRamUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var ramUsage = process.WorkingSet64 / (1024 * 1024); // Converti in MB
                var totalRam = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024); // Converti in MB
                return Math.Round((ramUsage / (double)totalRam) * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        private double GetDiskUsage()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
                var totalSize = drive.TotalSize;
                var freeSpace = drive.AvailableFreeSpace;
                return Math.Round(((totalSize - freeSpace) / (double)totalSize) * 100, 2);
            }
            catch
            {
                return 0;
            }
        }
    }
} 