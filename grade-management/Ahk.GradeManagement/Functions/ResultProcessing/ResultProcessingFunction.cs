using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Functions.StatusTracking;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Ahk.GradeManagement.ResultProcessing
{
    public class ResultProcessingFunction
    {
        private readonly IResultProcessor service;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger logger;

        public ResultProcessingFunction(IResultProcessor service, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory)
        {
            this.service = service;
            this.dateTimeProvider = dateTimeProvider;
            this.logger = loggerFactory.CreateLogger<ResultProcessingFunction>();
        }

        [Function("evaluation-result")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData request)
        {
            string token = request.Headers.GetValues("X-Ahk-Token").FirstOrDefault();
            string receivedSignature = request.Headers.GetValues("X-Ahk-Sha256").FirstOrDefault();
            string deliveryId = request.Headers.GetValues("X-Ahk-Delivery").FirstOrDefault();
            string dateStr = request.Headers.GetValues(HeaderNames.Date).FirstOrDefault();

            logger.LogInformation("evaluation-result request with X-Ahk-Delivery='{DeliveryId}', X-Ahk-Token = '{Token}'", deliveryId, token);

            if (string.IsNullOrEmpty(dateStr))
                return new BadRequestObjectResult(new { error = "Date header missing" });
            if (!DateTime.TryParseExact(dateStr, "R", provider: System.Globalization.CultureInfo.InvariantCulture, style: System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
                return new BadRequestObjectResult(new { error = "Date header value not valid RFC1123 string" });
            var now = dateTimeProvider.GetUtcNow();
            if (date < now.AddMinutes(-10) || date > now.AddMinutes(10))
                return new BadRequestObjectResult(new { error = "Date header value is not close enough to current date" });

            if (string.IsNullOrEmpty(receivedSignature))
                return new BadRequestObjectResult(new { error = "X-Ahk-Sha256 header missing" });

            if (string.IsNullOrEmpty(token))
                return new BadRequestObjectResult(new { error = "X-Ahk-Token header missing" });
            var secret = await service.GetSecretForTokenAsync(token);
            if (string.IsNullOrEmpty(secret))
                return new BadRequestObjectResult(new { error = "X-Ahk-Token invalid" });

            string requestBody = await HttpRequestDataExtensions.ReadAsStringAsync(request);
            if (!HmacSha256Validator.IsSignatureValid(request.Method, request.Url.ToString(), date, requestBody, receivedSignature, secret))
                return new BadRequestObjectResult(new { error = "X-Ahk-Sha256 signature not valid" });

            if (!PayloadReader.TryGetPayload<AhkProcessResult>(requestBody, out var requestDeserialized, out var deserializationError))
                return new BadRequestObjectResult(new { error = deserializationError });

            return await runCore(logger, deliveryId, requestDeserialized, date);
        }

        private async Task<IActionResult> runCore(ILogger logger, string deliveryId, AhkProcessResult requestDeserialized, DateTime date)
        {
            logger.LogInformation("evaluation-result request with X-Ahk-Delivery='{DeliveryId}' accepted, starting processing", deliveryId);

            try
            {
                await service.ProcessResultAsync(requestDeserialized, date);
                logger.LogInformation("evaluation-result request handled with success for X-Ahk-Delivery='{DeliveryId}'", deliveryId);
                return new OkResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "evaluation-result webhook failed for X-Ahk-Delivery='{DeliveryId}'", deliveryId);
                return new ObjectResult(new { error = ex.ToString() }) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
