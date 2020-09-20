using System.Threading.Tasks;

namespace OhSubtitle.Translations.Interfaces
{
    public interface ITranslationService
    {
        public Task<string> TranslateTextAsync(string orig);
        public string TranslateText(string orig);
    }
}