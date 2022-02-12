using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TorrentAPI.Domain;

namespace TorrentAPI
{
  public interface IRarbgApiClient
  {
    Task<string> GetResponseAsync(string url);

    Task<TorrentResults> GetResponseAsync(Settings settings);

    Task<string> GetInfoPageLink(string parameter);
  }

  public class RarbgApiClient : IRarbgApiClient
  {
    private static DateTime LastRequest;
    private readonly string BaseUrl;
    private readonly string AppID;

    private string Token;
    private DateTime TokenExpiration;
    private readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    private const string PassiveAggressiveUserAgent = "This is definitely a user agent, just so you know. Why you would not give me a response when none is specified is beyond me, though.";

    public RarbgApiClient(string baseUrl, string appID)
    {
      BaseUrl = baseUrl;
      AppID = appID;
    }

    private async Task GetNewTokenAsync()
    {
      Token = JsonConvert.DeserializeObject<TokenResponse>(await GetResponseAsync($"{BaseUrl}?get_token=get_token&app_id={AppID}")).Token;
      TokenExpiration = DateTime.Now.Add(TokenLifetime);
    }

    #region GetResponseAsync

    public async Task<TorrentResults> GetResponseAsync(Settings settings)
    {
      if (TokenExpiration < DateTime.Now)
        await GetNewTokenAsync();

      return JsonConvert.DeserializeObject<TorrentResults>(await GetResponseAsync($"{BaseUrl}?token={Token}&app_id={AppID}&{settings}"));
    }

    public async Task<string> GetInfoPageLink(string parameter)
    {
      if(TokenExpiration < DateTime.Now)
      {
        await GetNewTokenAsync();
      }

      var methodName = "https://torrentapi.org/redirect_to_info.php";

      var url = $"{methodName}?token={Token}&p={parameter}&app_id={AppID}";

      return url;
    }
    

    public async Task<string> GetResponseAsync(string url)
    {
      long waitTicks = LastRequest.AddSeconds(2).Ticks - DateTime.Now.Ticks;
      if (waitTicks > 0)
        await Task.Delay(TimeSpan.FromTicks(waitTicks));

      WebRequest request = WebRequest.Create(url);
      request.Headers.Add(HttpRequestHeader.UserAgent, PassiveAggressiveUserAgent);
      using (var response = request.GetResponse())
      using (var reader = new StreamReader(response.GetResponseStream()))
      {
        LastRequest = DateTime.Now;
        string result = reader.ReadToEnd();
        return result;
      }
    }

    #endregion

  }
}
