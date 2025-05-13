// Controllers/ChiamateController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using WebApplicationCentralino.Services;

namespace WebApplicationCentralino.Controllers
{
    public class ChiamateController : Controller
    {
        private readonly ChiamataService _chiamataService;

        public ChiamateController(ChiamataService chiamataService)
        {
            _chiamataService = chiamataService;
        }

        public async Task<IActionResult> Index(string dateFrom = null, string dateTo = null, double minDuration = 5)
        {
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            // Parsing dei parametri di data
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime fromDate))
            {
                fromDateParsed = fromDate;
            }
            else
            {
                // Se non specificato, usa oggi come default
                fromDateParsed = DateTime.Today;
                dateFrom = DateTime.Today.ToString("yyyy-MM-dd");
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime toDate))
            {
                // Aggiunge un giorno alla data finale per includerla completamente
                toDateParsed = toDate.AddDays(1).AddSeconds(-1);
            }
            else
            {
                // Se non specificato, usa oggi come default
                toDateParsed = DateTime.Today.AddDays(1).AddSeconds(-1);
                dateTo = DateTime.Today.ToString("yyyy-MM-dd");
            }

            // Ottieni le chiamate filtrate
            var chiamate = await _chiamataService.GetFilteredChiamateAsync(fromDateParsed, toDateParsed, minDuration);

            // Passa i valori alla vista per mantenere i filtri
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.MinDuration = minDuration;

            return View(chiamate);
        }
    }
}