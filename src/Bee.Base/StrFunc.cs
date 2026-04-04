using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Globalization;

namespace Bee.Base
{
    /// <summary>
    /// 字串處理函式庫。
    /// </summary>
    public static class StrFunc
    {
        /// <summary>
        /// 判斷是否為空字串，若為 null 也會視為空字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="isTrim">是否去除左右空白。</param>
        public static bool IsEmpty(string s, bool isTrim = true)
        {
            if (BaseFunc.IsNullOrDBNull(s))
                return true;
            if (isTrim)
                s = s.Trim();
            return (s == string.Empty);
        }

        /// <summary>
        /// 先轉型為字串再判斷是否為空字串，若為 null 也會視為空字串。
        /// </summary>
        /// <param name="s">字串。</param>
        public static bool IsEmpty(object s)
        {
            return IsEmpty(BaseFunc.CStr(s));
        }

        /// <summary>
        /// 判斷是否不為空字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="isTrim">是否去除左右空白。</param>
        public static bool IsNotEmpty(string s, bool isTrim = true)
        {
            return !IsEmpty(s, isTrim);
        }

        /// <summary>
        /// 先轉型為字串再判斷是否為空字串，若為 null 也會視為空字串。
        /// </summary>
        /// <param name="s">字串。</param>
        public static bool IsNotEmpty(object s)
        {
            return IsNotEmpty(BaseFunc.CStr(s));
        }

        /// <summary>
        /// 字串格式化。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="args">參數陣列。</param>
        public static string Format(string s, params object[] args)
        {
            return string.Format(s, args);
        }

        /// <summary>
        /// 字串格式化。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="row">資料列。</param>
        /// <param name="args">欄位名稱參數陣列。</param>
        public static string Format(string s, DataRow row, params string[] args)
        {
            object[] oValues;

            if (args == null) { return s; }

            oValues = new object[args.Length];
            for (int N1 = 0; N1 < args.Length; N1++)
                oValues[N1] = row[args[N1]];
            return Format(s, oValues);
        }

        /// <summary>
        /// 判斷二個字串是否相等。
        /// </summary>
        /// <param name="s1">第一個字串。</param>
        /// <param name="s2">第二個字串。</param>
        /// <param name="isTrim">比對前是否先去除左右空白。</param>
        /// <param name="ignoreCase">是否忽略大小寫。</param>
        public static bool IsEquals(string s1, string s2, bool isTrim = false, bool ignoreCase = true)
        {
            if (s1 == null)
                return (s2 == null);
            if (s2 == null)
                return false;

            if (isTrim)
            {
                s1 = s1.Trim();
                s2 = s2.Trim();
            }

            if (ignoreCase)
                return s1.Equals(s2, StringComparison.CurrentCultureIgnoreCase);
            else
                return s1.Equals(s2, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// 判斷字串是否等於比對的字串陣列中任一成員。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="values">比對的字串陣列。</param>
        public static bool IsEqualsOr(string s, params string[] values)
        {
            foreach (string value in values)
            {
                if (IsEquals(s, value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 將字串轉為大寫。
        /// </summary>
        /// <param name="s">字串。</param>
        public static string ToUpper(string s)
        {
            if (s == null)
                return string.Empty;
            else
                return s.ToUpper();
        }

        /// <summary>
        /// 將字串轉為小寫。
        /// </summary>
        /// <param name="s">字串。</param>
        public static string ToLower(string s)
        {
            if (s == null)
                return string.Empty;
            else
                return s.ToLower();
        }

        /// <summary>
        /// 字串取代。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="search">要搜尋的子字串。</param>
        /// <param name="replacement">取代子字串。</param>
        /// <param name="ignoreCase">是否忽略大小寫。</param>
        public static string Replace(string s, string search, string replacement, bool ignoreCase = true)
        {
            RegexOptions oOptions;

            // 空字串直接回傳
            if (IsEmpty(s)) { return string.Empty; }

            oOptions = (ignoreCase) ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.Replace(s, Regex.Escape(search), replacement, oOptions);
        }

        /// <summary>
        /// 將字串依分隔符號折解成陣列。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        public static string[] Split(string s, string delimiter)
        {
            if (IsEmpty(s))
                return new string[] { };
            else
                return s.Split(new string[] { delimiter }, StringSplitOptions.None);
        }

        /// <summary>
        /// 將字串依換行符號拆解成陣列。
        /// </summary>
        /// <param name="s">字串。</param>
        public static string[] SplitNewLine(string s)
        {
            if (StrFunc.IsEmpty(s))
                return new string[0];
            // 先將 \r 取代為空字串，然後用 \n 為分隔符號，拆解成陣列
            return s.Replace("\r", "").Split(new char[] { '\n' });
        }

        /// <summary>
        /// 由左邊開始尋找分隔符號，拆解成左右二字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        /// <param name="left">傳出左邊的字串。</param>
        /// <param name="right">傳出右邊的字串。</param>
        public static void SplitLeft(string s, string delimiter, out string left, out string right)
        {
            int iPos;

            iPos = Pos(s, delimiter);
            left = Left(s, iPos);
            right = Substring(s, iPos + 1);
        }

        /// <summary>
        /// 由右邊開始尋找分隔符號，拆解成左右二字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        /// <param name="left">傳出左邊的字串。</param>
        /// <param name="right">傳出右邊的字串。</param>
        public static void SplitRight(string s, string delimiter, out string left, out string right)
        {
            int iPos;

            iPos = PosRev(s, delimiter);
            left = Left(s, iPos);
            right = Substring(s, iPos + 1);
        }

        /// <summary>
        /// 加入字串及分隔符號。
        /// </summary>
        /// <param name="buffer">字串暫存區。</param>
        /// <param name="s">要加入的新字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        public static void Append(StringBuilder buffer, string s, string delimiter)
        {
            if (buffer.Length > 0)
                buffer.Append(delimiter);
            buffer.Append(s);
        }

        /// <summary>
        /// 依分隔符號合併二個字串。
        /// </summary>
        /// <param name="s1">第一個字串。</param>
        /// <param name="s2">第二個字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        public static string Merge(string s1, string s2, string delimiter)
        {
            if (IsNotEmpty(s1))
                s1 += delimiter;
            return s1 + s2;
        }

        /// <summary>
        /// 在暫存區加入分隔符號及字串。
        /// </summary>
        /// <param name="buffer">暫存區。</param>
        /// <param name="s">字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        public static void Merge(StringBuilder buffer, string s, string delimiter)
        {
            if (buffer.Length > 0)
                buffer.Append(delimiter);
            buffer.Append(s);
        }

        /// <summary>
        /// 取得字串左邊指定長度的子字串。 
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="length">長度。</param>
        public static string Left(string s, int length)
        {
            if (IsEmpty(s) || (length <= 0))
                return string.Empty;
            else
                return s.Substring(0, length);
        }

        /// <summary>
        /// 取得字串右邊指定長度的子字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="length">長度。</param>
        public static string Right(string s, int length)
        {
            int iStartIndex;

            if (IsEmpty(s) || (length <= 0)) { return string.Empty; }

            //計算擷取字串的起始位置
            iStartIndex = s.Length - length;
            //在字串中擷取指定起始位置後子字串
            return Substring(s, iStartIndex);
        }

        /// <summary>
        /// 字串左邊開頭是否符合指定字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="value">判斷符合的指定字串。</param>
        public static bool LeftWith(string s, string value)
        {
            if (IsEmpty(s))
                return false;
            else
                return s.StartsWith(value, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// 字串右方結尾是否符合指定字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="value">判斷符合的指定字串。</param>
        public static bool RightWith(string s, string value)
        {
            if (IsEmpty(s))
                return false;
            else
                return s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// 字串左邊去除指定長度的字元。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="length">長度。</param>
        public static string LeftCut(string s, int length)
        {
            return Substring(s, length);
        }

        /// <summary>
        /// 判斷字串左邊是否有指定字串，有得話則去除該指定字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="value">判斷的指定字串。</param>
        public static string LeftCut(string s, string value)
        {
            if (LeftWith(s, value))
                return LeftCut(s, value.Length);
            else
                return s;
        }

        /// <summary>
        /// 去除字串右邊指定長度的字串。 
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="length">要去除的長度。</param>
        public static string RightCut(string s, int length)
        {
            return Left(s, s.Length - length);
        }

        /// <summary>
        /// 判斷字串右邊是否有指定字串，有得話則去除該指定字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="value">判斷的指定字串。</param>
        public static string RightCut(string s, string value)
        {
            if (RightWith(s, value))
                return RightCut(s, value.Length);
            else
                return s;
        }

        /// <summary>
        /// 字串左右二邊去除指定字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="leftValue">左邊指定字串。</param>
        /// <param name="rightValue">右邊指定字串。</param>
        public static string LeftRightCut(string s, string leftValue, string rightValue)
        {
            string sValue;

            sValue = LeftCut(s, leftValue);
            sValue = RightCut(sValue, rightValue);
            return sValue;
        }

        /// <summary>
        /// 在字串中擷取指定起始位置後子字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="startIndex">以零為起始，擷取子字串的起始位置，若起始位置小於零，會強制設為零。</param>
        public static string Substring(string s, int startIndex)
        {
            if (IsEmpty(s)) { return string.Empty; }

            //計算擷取字串的起始位置，若起始位置小於零，則強制設為零
            if (startIndex < 0) { startIndex = 0; }
            return s.Substring(startIndex);
        }

        /// <summary>
        /// 在字串中擷取指定起始位置及長度的子字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="startIndex">以零為起始，擷取子字串的起始位置，若起始位置小於零，會強制設為零。</param>
        /// <param name="length">擷取長度。</param>
        public static string Substring(string s, int startIndex, int length)
        {
            if (IsEmpty(s) || (length <= 0)) { return string.Empty; }

            //計算擷取字串的起始位置，若起始位置小於零，則強制設為零
            if (startIndex < 0) { startIndex = 0; }

            //若擷取長度大於範圍，則取起始位置後的子字串，忽略擷取長度
            if ((startIndex + length) > s.Length)
                return s.Substring(startIndex);
            else
                return s.Substring(startIndex, length);
        }

        /// <summary>
        /// 在字串中判斷子字串的起始位置，若無指定子字串會傳回 -1。
        /// </summary>
        /// <param name="s">字串。</param>
        ///<param name="subString">子字串。</param>
        public static int Pos(string s, string subString)
        {
            if (IsEmpty(s))
                return -1;
            //忽略大小寫，故先轉為大寫後，再判斷子字串位置
            return ToUpper(s).IndexOf(ToUpper(subString));
        }

        /// <summary>
        /// 由字串右邊開始尋找子字串的位置，若無指定子字串會傳回 -1。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="subString">子字串。</param>
        public static int PosRev(string s, string subString)
        {
            if (IsEmpty(s))
                return -1;
            else
                return ToUpper(s).LastIndexOf(ToUpper(subString));
        }

        /// <summary>
        /// 判斷字串是否包含指定的子字串。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="subString">子字串。</param>
        public static bool Contains(string s, string subString)
        {
            if (Pos(s, subString) == -1)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 去除字串左右空白。
        /// </summary>
        /// <param name="s">字串。</param>
        public static string Trim(string s)
        {
            // 去除 ZERO WIDTH SPACE (U+200B) 與 ZERO WIDTH NO-BREAK SPACE (U+FEFF) 不可視字元
            // http://blog.miniasp.com/post/2014/01/15/C-Sharp-String-Trim-ZWSP-Zero-width-space.aspx
            if (s == null)
                return string.Empty;
            else
                return s.Trim().Trim(new char[] { '\uFEFF', '\u200B' });
        }

        /// <summary>
        /// 取得字串長度。
        /// </summary>
        /// <param name="s">字串。</param>        
        public static int Length(string s)
        {
            if (IsEmpty(s))
                return 0;
            else
                return s.Length;
        }

        /// <summary>
        /// 以指定字元填補左邊至指定長度。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="length">指定長度。</param>
        /// <param name="paddingChar">填補字元。</param>
        public static string PadLeft(string s, int length, char paddingChar)
        {
            return s.PadLeft(length, paddingChar);
        }

        /// <summary>
        /// 重覆指定字元組件的字串。
        /// </summary>
        /// <param name="number">重覆次數。</param>
        /// <param name="character">字元。</param>
        public static string Dup(int number, char character)
        {
            return PadLeft(string.Empty, number, character);
        }

        /// <summary>
        /// 模仿 VB 的 LikeString 方法，支援 *, ?, # 萬用字元的字串比對。
        /// </summary>
        /// <param name="source">來源字串。</param>
        /// <param name="pattern">比對模式，使用 VB Like 語法。</param>
        /// <param name="compareOption">比對選項（如 IgnoreCase）。</param>
        /// <returns>是否符合指定模式。</returns>
        public static bool Like(string source, string pattern, CompareOptions compareOption = CompareOptions.IgnoreCase)
        {
            if (source == null || pattern == null)
                return false;

            // Escape 正規式，再將萬用字元還原為 Regex 語法
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")     // *：任意長度字串
                .Replace(@"\?", ".")      // ?：任一字元
                .Replace(@"\#", "[0-9]")  // #：任一數字
                + "$";

            var options = RegexOptions.Compiled;
            if (compareOption.HasFlag(CompareOptions.IgnoreCase))
                options |= RegexOptions.IgnoreCase;

            return Regex.IsMatch(source, regexPattern, options);
        }

        /// <summary>
        /// 取得下一個流水號（支援 2~36 進位）。
        /// </summary>
        /// <param name="value">目前編號。</param>
        /// <param name="numberBase">流水號進位基底（2-36）。</param>
        /// <returns>下一個流水號。</returns>
        public static string GetNextId(string value, int numberBase)
        {
            if (numberBase < 2 || numberBase > 36)
                throw new ArgumentOutOfRangeException(nameof(numberBase), "Number base must be between 2 and 36.");

            var baseValues = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, numberBase);
            return GetNextId(value, baseValues);
        }

        /// <summary>
        /// 取得下一個流水號，依據自訂字元集遞增。
        /// </summary>
        /// <param name="value">目前編號。</param>
        /// <param name="baseValues">進位基底字元集。</param>
        /// <returns>下一個流水號。</returns>
        public static string GetNextId(string value, string baseValues)
        {
            if (string.IsNullOrEmpty(baseValues))
                throw new ArgumentException("Base values must not be null or empty.", nameof(baseValues));

            var digits = baseValues.ToCharArray();
            var baseLength = digits.Length;
            var current = StrFunc.Trim(value).ToCharArray();

            for (int i = current.Length - 1; i >= 0; i--)
            {
                var index = Array.IndexOf(digits, current[i]);

                if (index == -1)
                    throw new ArgumentException($"Invalid character '{current[i]}' in current ID.", nameof(value));

                if (index < baseLength - 1)
                {
                    current[i] = digits[index + 1];
                    return new string(current);
                }

                // overflow → reset to first digit
                current[i] = digits[0];
            }

            // 全部 overflow，進位補首碼（使用第一個非零字元）
            return digits[1] + new string(current);
        }
    }
}
