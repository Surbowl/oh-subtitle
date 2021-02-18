using System.Text.RegularExpressions;

namespace OhSubtitle.Helpers
{
    /// <summary>
    /// 字符串拓展方法
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 判断是否是单个英文单词
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsAEnglishWord(this string str)
        {
            str = str.Trim();
            if (str.Length > 45)
            {
                return false;
            }

            return new Regex(@"^([a-zA-Z]|-|\.|·|')+$").Match(str).Success;
        }
    }
}
