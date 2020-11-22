using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OhSubtitle.Services.TranslationGoogle
{
    /// <summary>
    /// 谷歌翻译
    /// </summary>
    public class GoogleTranslationService : ITranslationService
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
                return await "http://translate.google.cn/translate_a/single?client=gtx&dt=t&dj=1&ie=UTF-8&sl=auto"
                       .SetQueryParams(new
                       {
                           tl = HasChineseCharacters(orig) ? "auto" : "zh",
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
        /// 判断是否存在中文字符
        /// </summary>
        /// <param name="text"></param>
        /// <returns>存在中文字符</returns>
        bool HasChineseCharacters(string text)
        {
            if (text == null)
            {
                return false;
            }
            text = text.Trim();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c > 127 && !char.IsPunctuation(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}