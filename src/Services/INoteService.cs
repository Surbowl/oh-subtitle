using System.Threading;
using System.Threading.Tasks;

namespace OhSubtitle.Services
{
    /// <summary>
    /// 笔记服务
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        /// 记笔记
        /// </summary>
        /// <param name="orig">用户输入的原文</param>
        /// <param name="translated">翻译后的句子</param>
        /// <returns></returns>
        public Task WriteAsync(string orig, string translated, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清除笔记
        /// </summary>
        /// <returns></returns>
        public Task ClearAllAsync();
    }
}
