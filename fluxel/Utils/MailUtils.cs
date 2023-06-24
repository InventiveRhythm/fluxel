using System.Net.Mail;

namespace fluxel.Utils; 

public static class MailUtils {
    public static bool IsValidEmail(string email) {
        try
        {
            var m = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}