using WebApplicationCentralino.Models;

namespace WebApplicationCentralino.Services
{
    public interface IContattoService
    {
        Task<List<Contatto>> GetAllAsync();
        Task<List<Contatto>> SearchAsync(string? numero, string? ragione);
        Task<List<Contatto>> GetIncompleteAsync();
        Task<bool> AddAsync(Contatto contatto);
        Task<bool> UpdateAsync(string numero, Contatto contatto);
        Task<bool> DeleteAsync(string numero);
        Task<List<Chiamata>> GetCallsByNumberAsync(string numero);
    }

}
