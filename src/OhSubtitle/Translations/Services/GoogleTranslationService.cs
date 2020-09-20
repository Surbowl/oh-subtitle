using Flurl;
using Flurl.Http;
using OhSubtitle.Translations.Interfaces;
using OhSubtitle.Translations.Results;
using System.Threading.Tasks;

namespace OhSubtitle.Translations.Services
{
    public class GoogleTranslationService : ITranslationService
    {
        public async Task<string> TranslateTextAsync(string orig)
        {
            var result = await GetResultAsync(orig);
            return result.Result;
        }

        public string TranslateText(string orig)
        {
            return TranslateTextAsync(orig).GetAwaiter().GetResult();
        }

        bool HasChineseCharacters(string text)
        {
            if (text == null)
            {
                return false;
            }
            text = text.Trim();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 127)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<GoogleTranslationResult> GetResultAsync(string orig)
        {
            if (string.IsNullOrWhiteSpace(orig))
            {
                return new GoogleTranslationResult();
            }
            var result = await "http://translate.google.cn/translate_a/single"
            .SetQueryParams(new
            {
                client = "gtx",
                dt = 't',
                dj = 1,
                ie = "UTF-8",
                sl = "auto",
                tl = HasChineseCharacters(orig) ? "auto" : "zh",
                q = orig.TrimEnd()
            })
            .GetJsonAsync<GoogleTranslationResult>();
            return result ?? new GoogleTranslationResult();
        }
    }
}