using Flurl;
using Flurl.Http;
using OhSubtitle.Translations.Interfaces;
using OhSubtitle.Translations.Results;
using System;
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

        public async Task<GoogleTranslationResult> GetResultAsync(string orig)
        {
            if (string.IsNullOrWhiteSpace(orig))
            {
                return new GoogleTranslationResult();
            }
            try
            {
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
                return result;
            }
            catch (FlurlHttpException)
            {
                return new GoogleTranslationResult();
            }
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
                char c = text[i];
                if (c > 127 && !Char.IsPunctuation(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}