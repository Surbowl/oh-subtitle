using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OhSubtitle.Helpers;

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
    public static bool IsSingleEnglishWord(this string str)
    {
        str = str.Trim();
        if (str.Length > 45)
        {
            return false;
        }

        return new Regex(@"^([a-zA-Z]|-|\.|·|')+$").Match(str).Success;
    }

    /// <summary>
    /// 判断是否存在中/日/韩字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsContainsCJKCharacters(this string str)
    {
        if (str == null)
        {
            return false;
        }
        str = str.Trim();
        if (str.Length == 0)
        {
            return false;
        }
        var regex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
        return regex.IsMatch(str);
    }

    /// <summary>
    /// 判断是否存在日文字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns>存在中文字符</returns>
    public static bool IsContainsJapaneseCharacters(this string str)
    {
        if (str == null)
        {
            return false;
        }
        str = str.Trim();
        if (str.Length == 0)
        {
            return false;
        }
        var regex = new Regex(@"[\u3040-\u309F\u30A0-\u30FF]");
        return regex.IsMatch(str);
    }

    /// <summary>
    /// 32位MD5加密
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string MD5Encrypt32(this string text)
    {
        MD5 md5 = MD5.Create();
        byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(32);
        for (int i = 0; i < s.Length; i++)
        {
            sb.Append(s[i].ToString("x"));
        }
        return sb.ToString();
    }
}