using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using OpenReservation.ScheduleServices.Services;
using WeihanLi.Common;
using WeihanLi.Common.Helpers;
using WeihanLi.Extensions;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class JdGzfJob : AbstractJob
{
    public JdGzfJob(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
    {
    }

    public override string CronExpression => "10 15,23 * * *";

    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        var config = new JdGzfConfig();
        var configuration = scopeServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection("AppSettings:JdGzfConfig");
        configuration.Bind(config);
        Guard.NotNullOrEmpty(config.ApiUrl);

        var memoryCache = scopeServiceProvider.GetRequiredService<IMemoryCache>();
        var tokenCacheKey = "jdgzf:accessToken";
        if (!memoryCache.TryGetValue(tokenCacheKey, out string? token))
        {
            // login to get token
            var loginUrl = $"{config.ApiUrl}/api/token";
            using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
            request.Content = new FormUrlEncodedContent(
                [
                    new("loginType", "0"),
                    new("grant_type", "renter_login"),
                    new("username", GetEncryptedString(config.UserName, config.AesKey)),
                    new("password", GetEncryptedString(config.Password, config.AesKey))
                ]
            );
            foreach (var (headerName, headerValue) in GetRequestHeaders())
            {
                request.Headers.TryAddWithoutValidation(headerName, headerValue);
            }
            using var response = await HttpHelper.HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseObj = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
            var accessToken = responseObj?["access_token"]?.GetValue<string>();
            if (accessToken.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Invalid access token");
            }

            var expiresIn = responseObj?["expires_in"]?.GetValue<int>() ?? 604799;
            memoryCache.Set(tokenCacheKey, accessToken, TimeSpan.FromSeconds(expiresIn));

            token = accessToken;
        }
        
        var getRentingRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiUrl}/api/rent/get_rent_waiting_info");
        getRentingRequest.SetBearerToken(token!);
        foreach (var (headerName, headerValue) in GetRequestHeaders())
        {
            getRentingRequest.Headers.TryAddWithoutValidation(headerName, headerValue);
        }
        using var getRentingResponse = await HttpHelper.HttpClient.SendAsync(getRentingRequest, cancellationToken);
        getRentingResponse.EnsureSuccessStatusCode();
        var rentingInfoResponse = await getRentingResponse.Content.ReadFromJsonAsync<RentingInfoApiResponse>(cancellationToken);
        Logger.LogInformation("GetRentingInfo response: {@Response}", getRentingResponse);
        if (rentingInfoResponse?.Data is null)
        {
            throw new InvalidOperationException("Unexpected renting info response");
        }

        var notificationService = scopeServiceProvider.GetRequiredService<INotificationService>();
        var msg = $"您申请的小区【{rentingInfoResponse.Data.Community}】，房型【{rentingInfoResponse.Data.RoomKinds}】，当前在队列中处于第【{rentingInfoResponse.Data.SortNumber}】位，请耐心等待公租房公司为您分配房源。";
        await notificationService.SendNotificationAsync(msg);
    }

    private static IEnumerable<KeyValuePair<string, string>> GetRequestHeaders()
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 28800;
        var timestamp = t.ToString();
        yield return new("timestamp", timestamp);

        var nonce = "2341234";
        yield return new("nonce", nonce);

        var signature = HashHelper.GetHashedString(HashType.MD5, "jdgzf2020" + timestamp + nonce);
        yield return new("signature", signature);

        var userAgent =
            "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
        yield return new("User-Agent", userAgent);
    }

    private static string GetEncryptedString(string plainText, string key)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key.GetBytes();
        aesAlg.Mode = CipherMode.ECB;
        aesAlg.Padding = PaddingMode.PKCS7;
        
        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            cs.Write(plainBytes);
        }
        return Convert.ToHexString(ms.ToArray());
    }
}

file sealed class JdGzfConfig
{
    public string ApiUrl { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string AesKey { get; set; } = default!;
}

file sealed class RentingInfoApiResponse
{
    public bool Success { get; set; }
    public required string Code { get; set; }
    public required RentingInfo Data { get; set; }
}

file sealed class RentingInfo
{
    [JsonPropertyName("sortNum")]
    public required int SortNumber { get; set; }
    
    public required string Community { get; set; }
    
    public string? RoomKinds { get; set; }

}
