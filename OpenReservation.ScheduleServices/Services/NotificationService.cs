using WeihanLi.Common;
using WeihanLi.Common.Helpers;

namespace OpenReservation.ScheduleServices.Services;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(string msg);
}

public sealed class NotificationService : INotificationService
{
    private readonly string _webHookUrl = Guard.NotNullOrEmpty(Environment.GetEnvironmentVariable("DingBot_WebHookUrl"));

    public async Task<bool> SendNotificationAsync(string msg)
    {
        using var response = await HttpHelper.HttpClient.PostAsJsonAsync(_webHookUrl, new
        {
            msgtype = "text",
            text = new
            {
                content = $"{msg}\n{DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} [Amazing]"
            }
        });
        return response.IsSuccessStatusCode;
    }
}