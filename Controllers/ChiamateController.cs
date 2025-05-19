﻿// Controllers/ChiamateController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using CsvHelper;
using System.Globalization;
using WebApplicationCentralino.Services;
using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Controllers
{
    public class ChiamateController : Controller
    {
        private readonly ChiamataService _chiamataService;

        public ChiamateController(ChiamataService chiamataService)
        {
            _chiamataService = chiamataService;
        }

        public async Task<IActionResult> Index(string? dateFrom = null, string? dateTo = null, double minDuration = 5)
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
            ViewBag.UltimoAggiornamento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            return View(chiamate);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string? dateFrom = null, string? dateTo = null, double minDuration = 5, string? searchTerm = null)
        {
            // Riutilizziamo la stessa logica di filtro dell'Index
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime fromDate))
            {
                fromDateParsed = fromDate;
            }
            else
            {
                fromDateParsed = DateTime.Today;
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime toDate))
            {
                toDateParsed = toDate.AddDays(1).AddSeconds(-1);
            }
            else
            {
                toDateParsed = DateTime.Today.AddDays(1).AddSeconds(-1);
            }

            // Ottieni le chiamate filtrate
            var chiamate = await _chiamataService.GetFilteredChiamateAsync(fromDateParsed, toDateParsed, minDuration);

            // Applica il filtro di ricerca se specificato
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                chiamate = chiamate.Where(c =>
                    (c.NumeroChiamante?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.NumeroChiamato?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.RagioneSocialeChiamante?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.RagioneSocialeChiamato?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.TipoChiamata?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.Locazione?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();
            }

            // Crea il file Excel utilizzando ClosedXML
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Chiamate");

                // Aggiungi intestazioni
                worksheet.Cell(1, 1).Value = "Numero Chiamante";
                worksheet.Cell(1, 2).Value = "Ragione Sociale Chiamante";
                worksheet.Cell(1, 3).Value = "Numero Chiamato";
                worksheet.Cell(1, 4).Value = "Ragione Sociale Chiamato";
                worksheet.Cell(1, 5).Value = "Data Arrivo";
                worksheet.Cell(1, 6).Value = "Data Fine";
                worksheet.Cell(1, 7).Value = "Durata (sec)";
                worksheet.Cell(1, 8).Value = "Tipo Chiamata";
                worksheet.Cell(1, 9).Value = "Locazione";

                // Stile intestazioni
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Aggiungi dati
                int row = 2;
                foreach (var chiamata in chiamate)
                {
                    worksheet.Cell(row, 1).Value = chiamata.NumeroChiamante ?? "-";
                    worksheet.Cell(row, 2).Value = chiamata.RagioneSocialeChiamante ?? "-";
                    worksheet.Cell(row, 3).Value = chiamata.NumeroChiamato ?? "-";
                    worksheet.Cell(row, 4).Value = chiamata.RagioneSocialeChiamato ?? "-";
                    worksheet.Cell(row, 5).Value = chiamata.DataArrivoChiamata;
                    worksheet.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                    worksheet.Cell(row, 6).Value = chiamata.DataFineChiamata;
                    worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                    worksheet.Cell(row, 7).Value = chiamata.Durata.TotalSeconds;
                    worksheet.Cell(row, 8).Value = chiamata.TipoChiamata ?? "-";
                    worksheet.Cell(row, 9).Value = chiamata.Locazione ?? "-";
                    row++;
                }

                // Auto-dimensiona le colonne
                worksheet.Columns().AdjustToContents();

                // Genera il nome del file con il timestamp
                string fileName = $"Registro_Chiamate_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Salva in memoria
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    // Restituisci il file
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv(string? dateFrom = null, string? dateTo = null, double minDuration = 5, string? searchTerm = null)
        {
            // Riutilizziamo la stessa logica di filtro dell'Index
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime fromDate))
            {
                fromDateParsed = fromDate;
            }
            else
            {
                fromDateParsed = DateTime.Today;
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime toDate))
            {
                toDateParsed = toDate.AddDays(1).AddSeconds(-1);
            }
            else
            {
                toDateParsed = DateTime.Today.AddDays(1).AddSeconds(-1);
            }

            // Ottieni le chiamate filtrate
            var chiamate = await _chiamataService.GetFilteredChiamateAsync(fromDateParsed, toDateParsed, minDuration);

            // Applica il filtro di ricerca se specificato
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                chiamate = chiamate.Where(c =>
                    (c.NumeroChiamante?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.NumeroChiamato?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.RagioneSocialeChiamante?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.RagioneSocialeChiamato?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.TipoChiamata?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.Locazione?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();
            }

            // Prepara i dati per il CSV
            var records = chiamate.Select(c => new
            {
                NumeroChiamante = c.NumeroChiamante ?? "-",
                RagioneSocialeChiamante = c.RagioneSocialeChiamante ?? "-",
                NumeroChiamato = c.NumeroChiamato ?? "-",
                RagioneSocialeChiamato = c.RagioneSocialeChiamato ?? "-",
                DataArrivoChiamata = c.DataArrivoChiamata.ToString("dd/MM/yyyy HH:mm:ss"),
                DataFineChiamata = c.DataFineChiamata.ToString("dd/MM/yyyy HH:mm:ss"),
                DurataSecs = c.Durata.TotalSeconds.ToString("N0"),
                TipoChiamata = c.TipoChiamata ?? "-",
                Locazione = c.Locazione ?? "-"
            }).ToList();

            // Genera il nome del file con il timestamp
            string fileName = $"Registro_Chiamate_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // Salva in memoria
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.CurrentCulture))
            {
                // Scrivi i record
                csvWriter.WriteRecords(records);
                streamWriter.Flush();

                // Restituisci il file
                return File(
                    memoryStream.ToArray(),
                    "text/csv",
                    fileName);
            }
        }
    }
}