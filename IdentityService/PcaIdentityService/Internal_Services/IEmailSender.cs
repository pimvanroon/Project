using System.Threading.Tasks;

namespace PcaIdentityService.Internal_Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
