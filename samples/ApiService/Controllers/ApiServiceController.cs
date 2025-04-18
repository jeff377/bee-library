using Bee.Define;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiServiceController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] TApiServiceArgs args)
        {
            // 解密處理（如有加密）
            if (args.Encrypted)
            {
                args.Decrypt();
            }

            var result = new TApiServiceResult(args);

            try
            {
                // 根據 ProgID + Action 決定執行邏輯
                object output = HandleApiCall(args.ProgID, args.Action, args.Value);

                result.Value = output;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            // 加密處理（如需要）
            if (args.Encrypted)
            {
                result.Encrypt();
            }

            return Ok(result);
        }

        /// <summary>
        /// 實際的呼叫邏輯，可擴充 switch 或策略模式
        /// </summary>
        private object HandleApiCall(string progID, string action, object value)
        {
            if (progID == "LeaveForm" && action == "Submit")
            {
                // TODO: cast value to appropriate model (e.g., LeaveFormModel)
                // var form = JsonConvert.DeserializeObject<LeaveFormModel>(value.ToString());
                return new { Success = true, Message = "假單已送出" };
            }
            else if (progID == "LeaveForm" && action == "GetStatus")
            {
                return new { Status = "審核中", Approver = "王主管" };
            }

            throw new InvalidOperationException($"未支援的 ProgID 或 Action: {progID}/{action}");
        }
    }
}
