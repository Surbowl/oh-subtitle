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
    /// 谷歌中英文翻译
    /// </summary>
    public class GoogleEnglishTranslationService : ITranslationService
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
                return await "http://translate.google.com/translate_a/single?client=gtx&dt=t&dj=1&ie=UTF-8&sl=auto"
                       .SetQueryParams(new
                       {
                           tl = IsCJKUnifiedIdeographs(orig) ? "en" : "zh",
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
        /// 判断是否存在中/日/韩字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool IsCJKUnifiedIdeographs(string str)
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
            var regex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
            return regex.IsMatch(str);
        }
    }
}
