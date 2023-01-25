namespace OhSubtitle.Services;

/// <summary>
/// 笔记服务
/// </summary>
public interface INoteService
{
    /// <summary>
    /// 记笔记
    /// </summary>
    /// <param name="text">笔记内容</param>
    /// <returns></returns>
    public Task WriteAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除笔记
    /// </summary>
    /// <returns></returns>
    public Task ClearAllAsync();
}