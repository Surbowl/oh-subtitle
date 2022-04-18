using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Text;

namespace OhSubtitle.Services.Implements;

/// <summary>
/// 有道英文词典
/// </summary>
public class YoudaoEnglishDictionaryService : IDictionaryService
{
    /// <summary>
    /// 查询单词，失败时返回 Empty 字符串
    /// </summary>
    /// <param name="word">要查询的单词语句</param>
    /// <returns></returns>
    public async Task<string> QueryAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length > 45)
        {
            return string.Empty;
        }

        var apiResult = await GetApiResultAsync(word.Trim());
        if (apiResult == null)
        {
            return string.Empty;
        }
        return ApiResultToString(apiResult);
    }

    /// <summary>
    /// Get Api Result
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    async Task<JToken?> GetApiResultAsync(string word)
    {
        try
        {
            return await "http://dict.youdao.com/jsonapi?jsonversion=2&client=mobile&network=wifi&dicts=%7b%22count%22%3a99%2c%22dicts%22%3a%5b%5b%22ec%22%2c%22individual%22%5d%2c%5b%5d%2c%5b%5d%2c%5b%5d%2c%5b%5d%2c%5b%5d%2c%5b%5d%5d%7d"
                    .SetQueryParams(new
                    {
                        q = word
                    })
                    .GetJsonAsync<JToken>();
        }
        catch
        {
        }
        return null;
    }

    /// <summary>
    /// Api result to string
    /// </summary>
    /// <param name="jToken">Api result</param>
    /// <returns></returns>
    string ApiResultToString(JToken jToken)
    {
        return GetTrs(jToken) + GetWfs(jToken);
    }

    /// <summary>
    /// 获得词意
    /// </summary>
    /// <param name="jToken"></param>
    /// <returns></returns>
    string GetTrs(JToken jToken)
    {
        StringBuilder sb = new StringBuilder();

        // Try get Trs from ec token
        var ecTrs = jToken.SelectTokens("ec.word[0].trs[*].tr[0].l.i[0]")
                          .Select(t => t.Value<string>());
        foreach (var tr in ecTrs)
        {
            if (!string.IsNullOrWhiteSpace(tr))
            {
                sb.Append(tr + '\n');
            }
        };

        if (sb.Length == 0)
        {
            // Try get Trs from individual token
            var individualTrs = jToken.SelectTokens("individual.trs[*]");
            foreach (var tr in individualTrs)
            {
                var pos = tr.Value<string>("pos");
                var tran = tr.Value<string>("tran");
                if (!string.IsNullOrWhiteSpace(pos) && !string.IsNullOrWhiteSpace(tran))
                {
                    sb.Append(pos + ' ' + tran + '\n');
                }
            }
        }

        if (sb.Length > 3)
        {
            return sb.ToString().TrimStart();
        }
        return string.Empty;
    }

    /// <summary>
    /// 获得词性
    /// </summary>
    /// <param name="jToken"></param>
    /// <returns></returns>
    string GetWfs(JToken jToken)
    {
        StringBuilder sb = new StringBuilder();

        // Try get Wfs from ec token
        var ecWfs = jToken.SelectTokens("ec.word[0].wfs[*].wf");
        foreach (var wf in ecWfs)
        {
            var name = wf?.Value<string>("name");
            var value = wf?.Value<string>("value");
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
            {
                sb.Append(name + '：' + value + '、');
            }
        }

        if (sb.Length == 0)
        {
            // Try get Wfs from individual token
            var individualWfs = jToken.SelectTokens("individual.anagram.wfs[*]");
            foreach (var wf in individualWfs)
            {
                var name = wf?.Value<string>("name");
                var value = wf?.Value<string>("value");
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                {
                    sb.Append(name + '：' + value + '、');
                }
            }
        }

        if (sb.Length > 3)
        {
            sb.Remove(sb.Length - 1, 1)
              .Replace("第三人称单数", "三单")
              .Replace("名词复数", "复数");
            return sb.ToString().TrimStart();
        }
        return string.Empty;
    }
}