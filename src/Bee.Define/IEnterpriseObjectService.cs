namespace Bee.Define
{
    /// <summary>
    /// 提供企業系統中常用業務物件的統一存取服務介面。
    /// 透過快取機制加速資料讀取，未命中時自資料庫載入並反序列化為物件。
    /// 適用於組織結構、模組參數等需持久化與快取的資料。
    /// </summary>
    public interface IEnterpriseObjectService
    {

    }
}
