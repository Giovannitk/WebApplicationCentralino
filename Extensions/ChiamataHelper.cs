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
            var chiamateDaRestituire = new List<Chiamata>();

            // Raggruppa le chiamate per transferGroupId
            var gruppiTrasferimento = chiamate
                .Where(c => !string.IsNullOrEmpty(c.transferGroupId))
                .GroupBy(c => c.transferGroupId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Crea un dizionario per accesso rapido per UniqueID
            // Usa GroupBy per gestire eventuali duplicati e prendi il primo
            var chiamateById = chiamate
                .Where(c => !string.IsNullOrEmpty(c.UniqueID))
                .GroupBy(c => c.UniqueID!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var chiamata in chiamate)
            {
                // Se la chiamata è già stata processata, salta
                if (usate.Contains(chiamata.UniqueID!))
                    continue;

                // Caso 1: Chiamata con transferGroupId (è una chiamata trasferita)
                if (!string.IsNullOrEmpty(chiamata.transferGroupId) &&
                    chiamateById.TryGetValue(chiamata.transferGroupId, out var chiamataOriginale))
                {
                    // Unisci la chiamata originale con quella trasferita
                    var nuova = UnisciDueChiamate(chiamataOriginale, chiamata, logger);

                    chiamateDaRestituire.Add(nuova);
                    usate.Add(chiamata.UniqueID!);
                    usate.Add(chiamataOriginale.UniqueID!);

                    //logger?.LogInformation($"[ChiamataHelper] Chiamate unite: {chiamataOriginale.UniqueID} + {chiamata.UniqueID} -> {nuova.UniqueID}");
                    continue;
                }

                // Caso 2: Chiamata che potrebbe essere origine di trasferimento
                if (gruppiTrasferimento.TryGetValue(chiamata.UniqueID!, out var chiamateTrasferite))
                {
                    // Questa chiamata è l'origine, e ci sono chiamate trasferite
                    // Prendi la prima chiamata trasferita (dovrebbe essercene una sola per transferGroupId)
                    var chiamataTrasferita = chiamateTrasferite.First();

                    // Se la chiamata trasferita non è già stata processata
                    if (!usate.Contains(chiamataTrasferita.UniqueID!))
                    {
                        var nuova = UnisciDueChiamate(chiamata, chiamataTrasferita, logger);

                        chiamateDaRestituire.Add(nuova);
                        usate.Add(chiamata.UniqueID!);
                        usate.Add(chiamataTrasferita.UniqueID!);

                        //logger?.LogInformation($"[ChiamataHelper] Chiamate unite: {chiamata.UniqueID} + {chiamataTrasferita.UniqueID} -> {nuova.UniqueID}");
                        continue;
                    }
                }

                // Caso 3: Chiamata normale senza trasferimento
                chiamateDaRestituire.Add(chiamata);
                usate.Add(chiamata.UniqueID!);
                //logger?.LogInformation($"[ChiamataHelper] Chiamata normale aggiunta: {chiamata.UniqueID}");
            }

            return chiamateDaRestituire;
        }

        /// <summary>
        /// Unisce due chiamate collegate da trasferimento
        /// </summary>
        private static Chiamata UnisciDueChiamate(Chiamata chiamataOriginale, Chiamata chiamataTrasferita, ILogger? logger = null)
        {
            var nuova = new Chiamata();

            //logger?.LogInformation($"[ChiamataHelper] Unisco chiamate:");
            //logger?.LogInformation($"[ChiamataHelper] Originale: {DettagliChiamata(chiamataOriginale)}");
            //logger?.LogInformation($"[ChiamataHelper] Trasferita: {DettagliChiamata(chiamataTrasferita)}");

            // Caso 1: Chiamata in entrata trasferita
            if (chiamataOriginale.TipoChiamata == "Entrata")
            {
                nuova.NumeroChiamante = chiamataOriginale.NumeroChiamante;
                nuova.RagioneSocialeChiamante = chiamataOriginale.RagioneSocialeChiamante;
                nuova.Locazione = chiamataOriginale.Locazione;
                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;
                nuova.TipoChiamata = "Entrata"; // Mantieni come "Entrata" per il filtro
            }
            // Caso 2: Chiamata in uscita trasferita
            else if (chiamataOriginale.TipoChiamata == "Uscita")
            {
                nuova.NumeroChiamante = chiamataOriginale.NumeroChiamato; // Chi ha fatto la chiamata originale
                nuova.RagioneSocialeChiamante = chiamataOriginale.RagioneSocialeChiamato;
                nuova.Locazione = chiamataOriginale.LocazioneChiamato;
                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;
                nuova.TipoChiamata = "Uscita"; // Mantieni come "Uscita" per il filtro
            }
            else
            {
                // Fallback: usa la chiamata più vecchia come base
                var chiamataBase = chiamataOriginale.DataArrivoChiamata < chiamataTrasferita.DataArrivoChiamata
                    ? chiamataOriginale
                    : chiamataTrasferita;

                nuova.NumeroChiamante = chiamataBase.NumeroChiamante;
                nuova.RagioneSocialeChiamante = chiamataBase.RagioneSocialeChiamante;
                nuova.Locazione = chiamataBase.Locazione;
                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;
                nuova.TipoChiamata = chiamataBase.TipoChiamata; // Mantieni il tipo originale
            }

            // Imposta le date e altri campi
            nuova.DataArrivoChiamata = chiamataOriginale.DataArrivoChiamata < chiamataTrasferita.DataArrivoChiamata
                ? chiamataOriginale.DataArrivoChiamata
                : chiamataTrasferita.DataArrivoChiamata;

            nuova.DataFineChiamata = chiamataOriginale.DataFineChiamata > chiamataTrasferita.DataFineChiamata
                ? chiamataOriginale.DataFineChiamata
                : chiamataTrasferita.DataFineChiamata;

            nuova.UniqueID = chiamataOriginale.UniqueID + "+" + chiamataTrasferita.UniqueID;
            nuova.CampoExtra1 = "TRASFERIMENTO"; // Marca che è una chiamata con trasferimento
            nuova.transferGroupId = chiamataTrasferita.transferGroupId;

            //logger?.LogInformation($"[ChiamataHelper] Chiamata unita: {DettagliChiamata(nuova)}");

            return nuova;
        }

        private static string DettagliChiamata(Chiamata c)
        {
            return $"ID: {c.Id}, UniqueID: {c.UniqueID}, NumeroChiamante: {c.NumeroChiamante}, NumeroChiamato: {c.NumeroChiamato}, TipoChiamata: {c.TipoChiamata}, DataArrivo: {c.DataArrivoChiamata}, DataFine: {c.DataFineChiamata}, transferGroupId: {c.transferGroupId}";
        }
    }
}