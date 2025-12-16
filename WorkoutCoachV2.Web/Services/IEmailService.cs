using System.Threading.Tasks;

namespace WorkoutCoachV2.Web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
