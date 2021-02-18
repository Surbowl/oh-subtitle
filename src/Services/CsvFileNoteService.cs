using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OhSubtitle.Services
{
    /// <summary>
    /// 基于本地 Csv 文件的笔记服务
    /// </summary>
    public class CsvFileNoteService : INoteService
    {
        /// <summary>
        /// 表头
        /// </summary>
        const string TableHeader = "时间,原文,翻译";

        /// <summary>
        /// 笔记文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 笔记文件所在文件夹路径
        /// </summary>
        public string BaseDirectoryPath { get; set; }

        /// <summary>
        /// 完整文件路径
        /// </summary>
        public string FullFilePath => BaseDirectoryPath + '\\' + FileName;

        /// <summary>
        /// Create a CsvFileNoteService
        /// </summary>
        public CsvFileNoteService() : this("我的笔记MyNote.csv")
        {
        }

        /// <summary>
        /// Create a CsvFileNoteService
        /// </summary>
        /// <param name="fileName">笔记文件名</param>
        public CsvFileNoteService(string fileName) : this(Directory.GetCurrentDirectory(), fileName)
        {
        }

        /// <summary>
        /// Create a CsvFileNoteService
        /// </summary>
        /// <param name="baseDirectoryPath">笔记文件所在文件夹路径</param>
        /// <param name="fileName">笔记文件名</param>
        public CsvFileNoteService(string baseDirectoryPath, string fileName)
        {
            BaseDirectoryPath = baseDirectoryPath;
            FileName = fileName;
        }

        /// <summary>
        /// 记笔记
        /// </summary>
        /// <param name="orig">用户输入的原文</param>
        /// <param name="translated">翻译后的句子</param>
        /// <returns></returns>
        public async Task WriteAsync(string orig, string translated, CancellationToken cancellationToken = default)
        {
            orig = orig.Replace('\n', ' ')
                       .Replace(',', '，');
            translated = translated.Replace('\n', ' ')
                                   .Replace(',', '，');

            string appendText = File.Exists(FullFilePath) ? $"\n{DateTime.Now:yyyy-MM-dd HH:mm},{orig},{translated}"
                                                          : $"{TableHeader}\n{DateTime.Now:yyyy-MM-dd HH:mm},{orig},{translated}";
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            await File.AppendAllTextAsync(FullFilePath, appendText, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// 清除笔记
        /// </summary>
        /// <returns></returns>
        public async Task ClearAllAsync()
        {
            if (File.Exists(FullFilePath))
            {
                await File.WriteAllTextAsync(FullFilePath, TableHeader, Encoding.UTF8);
            }
        }
    }
}