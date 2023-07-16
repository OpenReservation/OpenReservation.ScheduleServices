using System.Text;
using AngleSharp.Html.Parser;
using Hangfire;
using OpenReservation.ScheduleServices.Services;
using WeihanLi.Common.Helpers;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class BethUniversityJob: AbstractJob
{
    private static readonly HtmlParser HtmlParser = new();
    
    public BethUniversityJob(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
    {
    }

    public override string CronExpression => Cron.Hourly(1);

    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider,
        CancellationToken cancellationToken)
    {
        var url = "http://aov.zzu.edu.cn/sss/zbisapi.dll/query7";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("impower", "2"),
                new("text1", "23410511151826"),
                new("B1", "查询")
            })
        };
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.82");
        request.Headers.Referrer = new Uri("http://ao.zzu.edu.cn/");
        using var response = await HttpHelper.HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("{Summary} {ResponseStatusCode}", "GetResponse failed", response.StatusCode);
            return;
        }

        var chineseEncoding = Encoding.GetEncoding("gb2312");
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream, chineseEncoding);
        var responseText = await reader.ReadToEndAsync(cancellationToken);
        var doc = await HtmlParser.ParseDocumentAsync(responseText, cancellationToken);
        var nodeText = doc.QuerySelector("div.lq-box")?.TextContent?.Trim();
        if (string.IsNullOrEmpty(nodeText))
        {
            Logger.LogWarning("{Summary}", "Node text or empty not found, maybe the structure changed");
            return;
        }
        if (nodeText.Contains("未检索到您的录取信息"))
        {
            Logger.LogInformation("{Summary}", "No related info found");
            return;
        }
        Logger.LogInformation("{Summary}", "Info found, please check http://ao.zzu.edu.cn/index.htm with 23410511151826");
        await scopeServiceProvider.GetRequiredService<INotificationService>()
            .SendNotificationAsync("录取状态发生变化，请进行检查");
    }
}