using UserAuthService.Entities;

namespace UserAuthService.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
}