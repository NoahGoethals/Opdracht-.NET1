using System.Threading.Tasks;

namespace WorkoutCoachV2.Web.Services
{
    // Contract voor mail-verzending zodat de app los staat van de concrete mail-provider .
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
