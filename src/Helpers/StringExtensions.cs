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

        /// <summary>
        /// 判断是否存在中文字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns>存在中文字符</returns>
        public static bool ContainsChineseCharacters(this string str)
        {
            if (str == null)
            {
                return false;
            }
            str = str.Trim();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c > 127 && !char.IsPunctuation(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
