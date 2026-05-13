using DevStation.Data.Models;
using DevStation.Services.Interfaces;

namespace DevStation.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private User? _currentUser;

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public bool IsAdmin => _currentUser?.Role == UserRole.Admin;

    public void SetUser(User? user) => _currentUser = user;
}
