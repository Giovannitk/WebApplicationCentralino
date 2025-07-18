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
        public static List<Chiamata> UnisciChiamateTrasferimento(
    List<Chiamata> chiamate,
    List<Contatto> contatti,
    ILogger? logger = null)
        {
            var usate = new HashSet<string>(); // UniqueID già usati in unione
            var chiamateDaRestituire = new List<Chiamata>();

            var numeriInterni = contatti
    .Where(c => c.Interno == 1 && !string.IsNullOrEmpty(c.NumeroContatto))
    .Select(c => c.NumeroContatto!)
    .ToHashSet();

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
                    var nuova = UnisciDueChiamate(chiamataOriginale, chiamata, numeriInterni, logger);

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
                        var nuova = UnisciDueChiamate(chiamata, chiamataTrasferita, numeriInterni, logger);

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
        private static Chiamata UnisciDueChiamate(
    Chiamata chiamataOriginale,
    Chiamata chiamataTrasferita,
    HashSet<string> numeriInterni,
    ILogger? logger = null)
        {
            var nuova = new Chiamata();

            var chiamanteOriginaleÈInterno = numeriInterni.Contains(chiamataOriginale.NumeroChiamante!);
            var chiamatoOriginaleÈInterno = numeriInterni.Contains(chiamataOriginale.NumeroChiamato!);
            var chiamatoTrasferitaÈInterno = numeriInterni.Contains(chiamataTrasferita.NumeroChiamato!);

            // Caso 1: Chiamata in entrata trasferita (es. esterno chiama → interno → altro interno)
            if (chiamataOriginale.TipoChiamata == "Entrata")
            {
                nuova.NumeroChiamante = chiamataOriginale.NumeroChiamante;
                nuova.RagioneSocialeChiamante = chiamataOriginale.RagioneSocialeChiamante;
                nuova.Locazione = chiamataOriginale.Locazione;

                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;

                nuova.TipoChiamata = "Entrata";
            }
            // Caso 2: Chiamata interna → uscita (interno riceve → passa → altro interno → esterno)
            else if (chiamataOriginale.TipoChiamata == "Interna" && chiamataTrasferita.TipoChiamata == "Uscita")
            {
                nuova.NumeroChiamante = chiamataOriginale.NumeroChiamato; // interno che ha ricevuto la chiamata
                nuova.RagioneSocialeChiamante = chiamataOriginale.RagioneSocialeChiamato;
                nuova.Locazione = chiamataOriginale.LocazioneChiamato;

                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;

                nuova.TipoChiamata = "Uscita";
            }
            // Caso 3: Chiamata in uscita trasferita tra interni
            else if (chiamataOriginale.TipoChiamata == "Uscita" && chiamataTrasferita.TipoChiamata == "Interna")
            {
                // Corretto: chiamante = interno che ha preso la chiamata (dalla chiamata interna), chiamato = esterno (dalla chiamata uscita)
                nuova.NumeroChiamante = chiamataTrasferita.NumeroChiamato; // interno che ha preso la chiamata
                nuova.RagioneSocialeChiamante = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.Locazione = chiamataTrasferita.LocazioneChiamato;

                nuova.NumeroChiamato = chiamataOriginale.NumeroChiamato; // esterno
                nuova.RagioneSocialeChiamato = chiamataOriginale.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataOriginale.LocazioneChiamato;

                nuova.TipoChiamata = "Uscita";
            }
            // Caso 4: Entrambe sono chiamate interne (interno → interno)
            else if (chiamataOriginale.TipoChiamata == "Interna" && chiamataTrasferita.TipoChiamata == "Interna")
            {
                nuova.NumeroChiamante = chiamataOriginale.NumeroChiamante;
                nuova.RagioneSocialeChiamante = chiamataOriginale.RagioneSocialeChiamante;
                nuova.Locazione = chiamataOriginale.Locazione;

                nuova.NumeroChiamato = chiamataTrasferita.NumeroChiamato;
                nuova.RagioneSocialeChiamato = chiamataTrasferita.RagioneSocialeChiamato;
                nuova.LocazioneChiamato = chiamataTrasferita.LocazioneChiamato;

                nuova.TipoChiamata = "Interna";
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

                nuova.TipoChiamata = chiamataBase.TipoChiamata;
            }

            // Date coerenti
            nuova.DataArrivoChiamata = chiamataOriginale.DataArrivoChiamata < chiamataTrasferita.DataArrivoChiamata
                ? chiamataOriginale.DataArrivoChiamata
                : chiamataTrasferita.DataArrivoChiamata;

            nuova.DataFineChiamata = chiamataOriginale.DataFineChiamata > chiamataTrasferita.DataFineChiamata
                ? chiamataOriginale.DataFineChiamata
                : chiamataTrasferita.DataFineChiamata;

            nuova.UniqueID = chiamataOriginale.UniqueID + "+" + chiamataTrasferita.UniqueID;
            nuova.CampoExtra4 = "TRASFERIMENTO";
            nuova.transferGroupId = chiamataTrasferita.transferGroupId;

            return nuova;
        }


        private static string DettagliChiamata(Chiamata c)
        {
            return $"ID: {c.Id}, UniqueID: {c.UniqueID}, NumeroChiamante: {c.NumeroChiamante}, NumeroChiamato: {c.NumeroChiamato}, TipoChiamata: {c.TipoChiamata}, DataArrivo: {c.DataArrivoChiamata}, DataFine: {c.DataFineChiamata}, transferGroupId: {c.transferGroupId}";
        }
    }
}