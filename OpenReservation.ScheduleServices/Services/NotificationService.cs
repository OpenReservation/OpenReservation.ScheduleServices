using WeihanLi.Common;
using WeihanLi.Common.Helpers;

namespace OpenReservation.ScheduleServices.Services;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(string msg);
}

public sealed class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    public NotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendNotificationAsync(string msg)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/notification/DingBot", new
        {
            text = msg,
            signature = "amazingdotnet"
        });
        return response.IsSuccessStatusCode;
    }
}