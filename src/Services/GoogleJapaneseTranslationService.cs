using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using OhSubtitle.Helpers;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OhSubtitle.Services
{
    /// <summary>
    /// 谷歌中日文翻译
    /// </summary>
    public class GoogleJapaneseTranslationService : ITranslationService
    {
        /// <summary>
        /// 翻译 支持中日文互译，翻译失败时返回 Empty 字符串
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
                return await "http://translate.google.com/translate_a/single?client=gtx&dt=t&dj=1&ie=UTF-8&sl=auto"
                       .SetQueryParams(new
                       {
                           tl = IsContainsJapaneseCharacters(orig) ? "zh" : "ja",
                           q = orig
                       })
                       .GetJsonAsync<JToken>();
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
            var sentences = jToken.SelectTokens("sentences[*].trans")
                                  .Select(s => s.Value<string>() ?? string.Empty);

            StringBuilder sb = new StringBuilder();
            foreach (var sentence in sentences)
            {
                sb.Append(sentence + ' ');
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 判断是否存在日文字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns>存在中文字符</returns>
        public bool IsContainsJapaneseCharacters(string str)
        {
            if (str == null)
            {
                return false;
            }
            str = str.Trim();
            if (str.Length == 0)
            {
                return false;
            }
            var regex = new Regex(@"[\u3040-\u309F\u30A0-\u30FF]");
            return regex.IsMatch(str);
        }
    }
}
