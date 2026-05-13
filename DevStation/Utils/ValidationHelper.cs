using System.Text.RegularExpressions;

namespace DevStation.Utils;

public static class ValidationHelper
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);

    public static bool IsValidEmail(string email) => EmailRegex.IsMatch(email);
    public static bool IsValidUsername(string username) => UsernameRegex.IsMatch(username);
    public static bool IsValidPassword(string password) => password.Length >= 6;
}
