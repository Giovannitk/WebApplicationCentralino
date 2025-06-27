using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationCentralino.Models;
using Microsoft.Extensions.Logging;

namespace WebApplicationCentralino.Extensions
{
    public static class ChiamataHelper
    {
        /// <summary>
        /// Unisce le chiamate collegate da trasferimento in una singola istanza rappresentativa, con log dettagliati.
        /// </summary>
        /// <param name="chiamate">Lista originale delle chiamate</param>
        /// <param name="logger">Logger opzionale per debug</param>
        /// <returns>Nuova lista con chiamate unite per trasferimento</returns>
        public static List<Chiamata> UnisciChiamateTrasferimento(List<Chiamata> chiamate, ILogger? logger = null)
        {
            var usate = new HashSet<string>(); // UniqueID già usati in unione
            var daRimuovere = new HashSet<string>(); // UniqueID delle gemelle da rimuovere
            var chiamateDaRestituire = new List<Chiamata>();
            var chiamateById = chiamate
                .Where(c => !string.IsNullOrEmpty(c.UniqueID))
                .GroupBy(c => c.UniqueID!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var chiamata in chiamate)
            {
                if (!string.IsNullOrEmpty(chiamata.transferGroupId) && chiamateById.TryGetValue(chiamata.transferGroupId, out var gemella))
                {
                    if (!usate.Contains(chiamata.UniqueID!))
                    {
                        //logger?.LogInformation($"[ChiamataHelper] Unisco chiamate con UniqueID={chiamata.UniqueID} e UniqueID gemella={gemella.UniqueID} (CampoExtra2={chiamata.transferGroupId})");
                        //logger?.LogInformation($"[ChiamataHelper] Dettagli chiamata 1: {DettagliChiamata(chiamata)}");
                        //logger?.LogInformation($"[ChiamataHelper] Dettagli chiamata 2: {DettagliChiamata(gemella)}");
                        var nuova = new Chiamata();
                        // Caso 1: Entrata (l'esterno chiama, poi trasferito)
                        if (gemella.TipoChiamata == "Entrata")
                        {
                            nuova.NumeroChiamante = gemella.NumeroChiamante;
                            nuova.RagioneSocialeChiamante = gemella.RagioneSocialeChiamante;
                            nuova.Locazione = gemella.Locazione;
                            nuova.NumeroChiamato = chiamata.NumeroChiamato;
                            nuova.RagioneSocialeChiamato = chiamata.RagioneSocialeChiamato;
                            nuova.LocazioneChiamato = chiamata.LocazioneChiamato;
                            nuova.DataArrivoChiamata = gemella.DataArrivoChiamata < chiamata.DataArrivoChiamata ? gemella.DataArrivoChiamata : chiamata.DataArrivoChiamata;
                            nuova.DataFineChiamata = chiamata.DataFineChiamata > gemella.DataFineChiamata ? chiamata.DataFineChiamata : gemella.DataFineChiamata;
                            nuova.TipoChiamata = "Entrata+Trasferimento";
                        }
                        // Caso 2: Uscita (interno chiama esterno, poi trasferisce)
                        else if (gemella.TipoChiamata == "Uscita")
                        {
                            nuova.NumeroChiamante = gemella.NumeroChiamato; // chiamato della prima istanza
                            nuova.RagioneSocialeChiamante = gemella.RagioneSocialeChiamato;
                            nuova.Locazione = gemella.LocazioneChiamato;
                            nuova.NumeroChiamato = chiamata.NumeroChiamato;
                            nuova.RagioneSocialeChiamato = chiamata.RagioneSocialeChiamato;
                            nuova.LocazioneChiamato = chiamata.LocazioneChiamato;
                            nuova.DataArrivoChiamata = gemella.DataArrivoChiamata < chiamata.DataArrivoChiamata ? gemella.DataArrivoChiamata : chiamata.DataArrivoChiamata;
                            nuova.DataFineChiamata = chiamata.DataFineChiamata > gemella.DataFineChiamata ? chiamata.DataFineChiamata : gemella.DataFineChiamata;
                            nuova.TipoChiamata = "Uscita+Trasferimento";
                        }
                        else
                        {
                            // fallback: copia la chiamata più vecchia come base
                            nuova = gemella.DataArrivoChiamata < chiamata.DataArrivoChiamata ? gemella : chiamata;
                        }
                        nuova.UniqueID = chiamata.UniqueID + "+" + gemella.UniqueID;
                        nuova.CampoExtra1 = chiamata.CampoExtra1;
                        nuova.transferGroupId = chiamata.transferGroupId;
                        chiamateDaRestituire.Add(nuova);
                        usate.Add(chiamata.UniqueID!);
                        usate.Add(gemella.UniqueID!);
                        daRimuovere.Add(gemella.UniqueID!);
                        //logger?.LogInformation($"[ChiamataHelper] Chiamata unita aggiunta: {DettagliChiamata(nuova)}");
                        continue;
                    }
                }
                if (!usate.Contains(chiamata.UniqueID!))
                {
                    chiamateDaRestituire.Add(chiamata);
                    usate.Add(chiamata.UniqueID!);
                    //logger?.LogInformation($"[ChiamataHelper] Chiamata non unita aggiunta normalmente: {DettagliChiamata(chiamata)}");
                }
            }
            // Rimuovi dalla lista tutte le chiamate gemelle già unite
            chiamateDaRestituire = chiamateDaRestituire
                .Where(c => !daRimuovere.Contains(c.UniqueID!))
                .ToList();
            return chiamateDaRestituire;
        }

        private static string DettagliChiamata(Chiamata c)
        {
            return $"ID: {c.Id}, UniqueID: {c.UniqueID}, NumeroChiamante: {c.NumeroChiamante}, NumeroChiamato: {c.NumeroChiamato}, TipoChiamata: {c.TipoChiamata}, DataArrivo: {c.DataArrivoChiamata}, DataFine: {c.DataFineChiamata}, CampoExtra2: {c.transferGroupId}";
        }
    }
}