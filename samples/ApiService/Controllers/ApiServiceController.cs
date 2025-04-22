using Bee.Api.Core;
using Bee.Base;
using Bee.Define;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiServiceController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            var apiKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();
            using var reader = new StreamReader(HttpContext.Request.Body);
            string json = await reader.ReadToEndAsync();
            var args = SerializeFunc.JsonToObject<TApiServiceArgs>(json);
            if (args == null)
            {
                var result = new TApiServiceResult()
                {
                    Message = "無法解析傳入的 JSON 資料"
                };
                return BadRequest(result);
            }

            try
            {

                var result = Execute(args);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = new TApiServiceResult(args)
                {
                    Message = ex.Message
                };
                // 建議回傳 500 錯誤
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        public TApiServiceResult Execute(TApiServiceArgs args)
        {
            args.Decrypt();  // 傳入資料進行解密
            var executor = new TApiServiceExecutor(Guid.Empty);
            var result = executor.Execute(args);
            result.Encrypt();  // 傳出結果進行加密
            return result;
        }
    }
}
