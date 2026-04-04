using System;
using System.Linq.Expressions;

namespace Bee.Base
{
    /// <summary>
    /// 提供屬性或欄位的完整路徑解析工具，
    /// 可將 Lambda 表示式轉換為「類別.成員」字串，支援巢狀結構。
    /// </summary>
    public static class MemberPath
    {
        /// <summary>
        /// 取得屬性或欄位的完整路徑 (支援巢狀)。
        /// </summary>
        /// <typeparam name="T">屬性或欄位類型。</typeparam>
        /// <param name="expression">Lambda，例如：() => Config.Database.ServerName。</param>
        public static string Of<T>(Expression<Func<T>> expression)
        {
            if (expression.Body is MemberExpression member)
                return GetFullPath(member);

            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
                return GetFullPath(innerMember);

            throw new ArgumentException("Expression must be a property or field.", nameof(expression));
        }

        private static string GetFullPath(MemberExpression member)
        {
            // 有父層就遞迴組路徑
            if (member.Expression is MemberExpression parent)
                return $"{GetFullPath(parent)}.{member.Member.Name}";

            // 到最上層（static class 或 instance 類別）
            return $"{member.Member.DeclaringType?.Name}.{member.Member.Name}";
        }
    }

}
