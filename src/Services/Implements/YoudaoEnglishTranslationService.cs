using Flurl.Http;
using Newtonsoft.Json.Linq;
using OhSubtitle.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace OhSubtitle.Services.Implements;

/// <summary>
/// 有道中英文翻译
/// </summary>
public class YoudaoEnglishTranslationService : ITranslationService
{
    /// <summary>
    /// 翻译 支持中英文互译，翻译失败时返回 Empty 字符串
    /// </summary>
    /// <param name="orig">要翻译的语句</param>
    /// <returns></returns>
    public async Task<string> TranslateAsync(string orig)
    {
        if (string.IsNullOrWhiteSpace(orig))
        {
            return string.Empty;
        }
        var result = await GetApiResultAsync(orig.TrimEnd());
        if (result == null)
        {
            return string.Empty;
        }

        return ApiResultToString(result);
    }

    /// <summary>
    /// Get Api Result
    /// </summary>
    /// <param name="orig">要翻译的语句</param>
    /// <returns></returns>
    public async Task<JToken?> GetApiResultAsync(string orig)
    {
        try
        {
            return await "http://fanyi.youdao.com/translate?smartresult=dict&smartresult=rule"
                   .WithHeaders(new
                   {
                       Referer = "http://fanyi.youdao.com/",
                       Content_Type = "application/x-www-form-urlencoded",
                       User_Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.75 Safari/537.36",
                   })
                   .PostUrlEncodedAsync(new
                   {
                       i = orig,
                       from = "AUTO",
                       to = orig.IsContainsCJKCharacters() ? "en" : "zh-CHS",
                       doctype = "json"
                   })
                   .ReceiveJson<JToken>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Api result to string
    /// </summary>
    /// <param name="jToken">Api result</param>
    /// <returns></returns>
    string ApiResultToString(JToken jToken)
    {
        try
        {
            var arr = jToken.SelectTokens("translateResult[*][*].tgt").ToArray();
            if (arr.Length == 0)
            {
                return string.Empty;
            }
            return arr[0].Value<string>() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 32位MD5加密
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string MD5Encrypt32(string text)
    {
        MD5 md5 = MD5.Create();
        byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(32);
        for (int i = 0; i < s.Length; i++)
        {
            sb.Append(s[i].ToString("x"));
        }
        return sb.ToString();
    }
}