using System.Threading.Tasks;

namespace OhSubtitle.Services.Interfaces
{
    /// <summary>
    /// 词典服务
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        /// 查询单词，失败时返回 Empty 字符串
        /// </summary>
        /// <param name="word">要查询的单词语句</param>
        /// <returns></returns>
        public Task<string> QueryAsync(string word);
    }
}
