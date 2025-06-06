using System.Collections.Generic;
using System.Linq;

namespace WebApplicationCentralino.Managers
{
    public static class ComuniManager
    {
        private static readonly Dictionary<string, string> _comuni = new Dictionary<string, string>
        {
            { "Alì", "Ali'" },
            { "Alì Terme", "Ali' Terme" },
            { "Antillo", "Antillo" },
            { "Barcellona Pozzo di Gotto", "Barcellona Pozzo di Gotto" },
            { "Basicò", "Basico'" },
            { "Brolo", "Brolo" },
            { "Capizzi", "Capizzi" },
            { "Capri Leone", "Capri Leone" },
            { "Capo D'Orlando", "Capo D'Orlando" },
            { "Casalvecchio Siculo", "Casalvecchio Siculo" },
            { "Falcone", "Falcone" },
            { "Forza d'Agrò", "Forza d'Agro'" },
            { "Furci Siculo", "Furci Siculo" },
            { "Furnari", "Furnari" },
            { "Gallodoro", "Gallodoro" },
            { "Itala", "Itala" },
            { "Leni", "Leni" },
            { "Letojanni", "Letojanni" },
            { "Limina", "Limina" },
            { "Longi", "Longi" },
            { "Mandanici", "Mandanici" },
            { "Mazzarrà S. Andrea", "Mazzarra S. Andrea" },
            { "Messina", "Messina" },
            { "Milazzo", "Milazzo" },
            { "Mistretta", "Mistretta" },
            { "Mongiuffi Melia", "Mongiuffi Melia" },
            { "Montalbano Elicona", "Montalbano Elicona" },
            { "Motta Camastra", "Motta Camastra" },
            { "Nizza di Sicilia", "Nizza di Sicilia" },
            { "Novara di Sicilia", "Novara di Sicilia" },
            { "Oliveri", "Oliveri" },
            { "Reitano", "Reitano" },
            { "Pace del Mela", "Pace del Mela" },
            { "Patti", "Patti" },
            { "Roccafiorita", "Roccafiorita" },
            { "Roccalumera", "Roccalumera" },
            { "Sambuca", "Sambuca" },
            { "Santa Lucia del Mela", "Santa Lucia del Mela" },
            { "Saponara", "Saponara" },
            { "Scaletta Zanclea", "Scaletta Zanclea" },
            { "Spadafora", "Spadafora" },
            { "Terme Vigliatore", "Terme Vigliatore" },
            { "Torrenova", "Torrenova" },
            { "Tripi", "Tripi" },
            { "Venetico", "Venetico" }
        };

        public static Dictionary<string, string> GetComuniDictionary()
        {
            return _comuni;
        }

        public static List<string> GetComuniList()
        {
            return _comuni.Keys.Select(k => $"Comune di {k}").ToList();
        }

        public static string GetDatabaseValue(string displayValue)
        {
            if (_comuni.TryGetValue(displayValue, out string dbValue))
            {
                return dbValue;
            }
            return displayValue;
        }

        public static string GetDisplayValue(string dbValue)
        {
            var pair = _comuni.FirstOrDefault(x => x.Value == dbValue);
            return pair.Key ?? dbValue;
        }
    }
} 