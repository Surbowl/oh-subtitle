using Flurl.Http;
using Newtonsoft.Json.Linq;
using OhSubtitle.Helpers;

namespace OhSubtitle.Services.Implements;

/// <summary>
/// 有道中日文翻译
/// </summary>
public class YoudaoJapaneseTranslationService : ITranslationService
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
        // https://github.com/Chinese-boy/Many-Translaters/blob/master/%E6%9C%89%E9%81%93%E7%BF%BB%E8%AF%91-%E6%9C%89%E5%8F%8D%E7%88%AC.py
        // https://blog.csdn.net/hujingshuang/article/details/80177784

        var ts = (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() * 1000).ToString();
        var salt = ts + new Random().Next(0, 10).ToString();
        var sign = ("fanyideskweb" + orig + salt + "Ygy_4c=r#e#4EX^NUGUc5").MD5Encrypt32();

        try
        {
            return await "https://fanyi.youdao.com/translate_o?smartresult=dict&smartresult=rule"
                   .WithHeaders(new
                   {
                       Cookie = "OUTFOX_SEARCH_USER_ID=-1065578080@10.169.0.81; JSESSIONID=aaa_gOxm6OLYR8dTXHDay; OUTFOX_SEARCH_USER_ID_NCOO=2137448902.0858676; fanyi-ad-id=305426; fanyi-ad-closed=0; JSESSIONID=abcZXHeFwaI2t4ESKNDay;",
                       Referer = "https://fanyi.youdao.com/",
                       Content_Type = "application/x-www-form-urlencoded",
                       User_Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.75 Safari/537.36",
                   })
                   .PostUrlEncodedAsync(new
                   {
                       i = orig,
                       from = "AUTO",
                       to = orig.IsContainsJapaneseCharacters() ? "zh-CHS" : "ja",
                       smartresult = "dict",
                       client = "fanyideskweb",
                       doctype = "json",
                       version = "2.1",
                       keyfrom = "fanyi.web",
                       action = "FY_BY_CLICKBUTTION",
                       typoResult = "true",
                       bv = "7e897500afcde4988ba75227fa754c55",
                       lts = ts,
                       salt = salt,
                       sign = sign
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
}