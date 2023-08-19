namespace fluxel.API.Utils;

using BCrypt.Net;

public abstract class PasswordUtils {
    public static string HashPassword(string password) {
        return BCrypt.HashPassword(password);
    }

    public static bool VerifyPassword(string password, string hash) {
        return BCrypt.Verify(password, hash);
    }
}
