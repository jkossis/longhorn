using Longhorn.Models;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Longhorn.Services;

public interface INotificationService
{
    Task SendNotification(string title, string body);
}

public class NotificationService(HttpClient httpClient) : INotificationService
{
    public async Task SendNotification(string title, string body)
    {
        var response = await httpClient.PostAsync(
            "/notify/pushover",
            new StringContent(
                JsonSerializer.Serialize(new Notification { Title = title, Body = body }),
                Encoding.UTF8,
                MediaTypeNames.Application.Json
            )
        );

        response.EnsureSuccessStatusCode();
    }
}