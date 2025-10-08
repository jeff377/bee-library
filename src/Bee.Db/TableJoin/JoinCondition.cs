namespace Bee.Db
{
    /// <summary>
    /// Join 條件（例如 A.EmployeeId = B.Id）。
    /// </summary>
    public class JoinCondition
    {
        /// <summary>
        /// 左側欄位（包含別名）。
        /// </summary>
        public string LeftField { get; set; }

        /// <summary>
        /// 右側欄位（包含別名）。
        /// </summary>
        public string RightField { get; set; }

        /// <summary>
        /// 轉換為 SQL 條件字串。
        /// </summary>
        /// <returns>條件字串，如 A.EmployeeID = B.Id。</returns>
        public string ToSql()
        {
            return $"{LeftField} = {RightField}";
        }
    }
}
