using DevStation.Data.Models;

namespace DevStation.Services.Interfaces;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    void SetUser(User? user);
}
