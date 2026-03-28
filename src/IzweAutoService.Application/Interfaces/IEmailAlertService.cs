namespace IzweAutoService.Application.Interfaces;

public interface IEmailAlertService
{
    Task SendAlertAsync(string subject, string body);
}
