using System.Threading.Tasks;

namespace OhSubtitle.Services
{
    /// <summary>
    /// 翻译服务
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// 翻译语句，翻译失败时返回 Empty 字符串
        /// </summary>
        /// <param name="orig">要翻译的语句</param>
        /// <returns></returns>
        public Task<string> TranslateAsync(string orig);
    }
}