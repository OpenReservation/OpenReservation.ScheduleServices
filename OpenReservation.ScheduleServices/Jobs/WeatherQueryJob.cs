using OpenReservation.ScheduleServices.Services;
using WeihanLi.Common.Helpers;
using WeihanLi.Extensions;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class WeatherQueryJob(IServiceProvider serviceProvider)
    : AbstractJob(serviceProvider)
{
    public override string CronExpression => "0 15,23 * * *";

    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        var weatherQueryUrl = scopeServiceProvider.GetRequiredService<IConfiguration>()["QWeatherApiQueryUrl"];
        if (string.IsNullOrEmpty(weatherQueryUrl))
        {
            Logger.LogWarning("No valid weather query url, skip weather query");
            return;
        }

        var response = await HttpHelper.HttpClient.GetFromJsonAsync<WeatherResponse>(weatherQueryUrl, cancellationToken);
        if ("200" != response?.Code)
        {
            Logger.LogError("Invalid response, response code: {ResponseCode}", response?.Code);
            return;
        }
        if (response.Daily is not { Length: > 0 })
        {
            Logger.LogError("No data found");
            return;
        }

        var text = response.Daily.Select(x => x)
            .StringJoin($"{Environment.NewLine}{new string('=', 20)}{Environment.NewLine}");
        await scopeServiceProvider.GetRequiredService<INotificationService>()
            .SendNotificationAsync($"天气预报{Environment.NewLine}{text}");
    }
}
/// <summary>
/// 天气响应结构
/// https://dev.qweather.com/docs/api/weather/weather-daily-forecast/
/// </summary>
file sealed class WeatherResponse
{
    /// <summary>
    /// response code, https://dev.qweather.com/docs/resource/status-code/
    /// 200	请求成功
    /// 204	请求成功，但你查询的地区暂时没有你需要的数据。
    /// 400	请求错误，可能包含错误的请求参数或缺少必选的请求参数。
    /// 401	认证失败，可能使用了错误的KEY、数字签名错误、KEY的类型错误（如使用SDK的KEY去访问Web API）。
    /// 402	超过访问次数或余额不足以支持继续访问服务，你可以充值、升级访问量或等待访问量重置。
    /// 403	无访问权限，可能是绑定的PackageName、BundleID、域名IP地址不一致，或者是需要额外付费的数据。
    /// 404	查询的数据或地区不存在。
    /// 429	超过限定的QPM（每分钟访问次数），请参考QPM说明
    /// 500	无响应或超时，接口服务异常请联系我们
    /// </summary>
    public string? Code { get; set; }
    public WeatherDailyResponse[]? Daily { get; set; }
}

file sealed class WeatherDailyResponse
{
    /// <summary>
    /// 预报日期
    /// </summary>
    public required string FxDate { get; set; }
    
    /// <summary>
    /// 日出时间
    /// </summary>
    public string? Sunrise { get; set; }
    /// <summary>
    /// 日落时间
    /// </summary>
    public string? Sunset { get; set; }
    
    /// <summary>
    /// 当天最高温度
    /// </summary>
    public string? TempMax { get; set; }
    /// <summary>
    /// 当天最低温度
    /// </summary>
    public string? TempMin { get; set; }
    
    public string? IconDay { get; set; }
    /// <summary>
    /// 预报白天天气状况文字描述，包括阴晴雨雪等天气状态的描述
    /// </summary>
    public string? TextDay { get; set; }
    public string? IconNight { get; set; }
    /// <summary>
    /// 预报晚间天气状况文字描述，包括阴晴雨雪等天气状态的描述
    /// </summary>
    public string? TextNight { get; set; }
    
    /// <summary>
    /// 相对湿度，百分比数值
    /// </summary>
    public string? Humidity { get; set; }

    /// <summary>
    /// 紫外线强度指数
    /// </summary>
    public string? UvIndex { get; set; }
    
    public override string ToString()
    {
        return $$"""
               日 期：{{FxDate}}
               气温： {{TempMin}} - {{TempMax}}
               白天：{{TextDay}}
               夜晚：{{TextNight}}
               湿度： {{Humidity}}
               紫外线强度：{{UvIndex}}
               日出时间：{{Sunrise}}
               日落时间：{{Sunset}}
               """;
    }
}