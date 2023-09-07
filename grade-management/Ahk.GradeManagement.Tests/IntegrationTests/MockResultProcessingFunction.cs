using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using Ahk.GradeManagement.ResultProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;
using Microsoft.Azure.WebJobs;

namespace Ahk.GradeManagement.Tests.IntegrationTests
{
    internal class MockResultProcessingFunction
    {
        private readonly IResultProcessor service;
        private readonly IDateTimeProvider dateTimeProvider;

        public MockResultProcessingFunction(IResultProcessor service, IDateTimeProvider dateTimeProvider)
        {
            this.service = service;
            this.dateTimeProvider = dateTimeProvider;
        }
        [FunctionName("evaluation-result")]
        public async Task<IActionResult> Run(HttpRequest request, ILogger logger)
        {
            string token = request.Headers.GetValueOrDefault("X-Ahk-Token");
            string receivedSignature = request.Headers.GetValueOrDefault("X-Ahk-Sha256");
            string deliveryId = request.Headers.GetValueOrDefault("X-Ahk-Delivery");
            string dateStr = request.Headers.GetValueOrDefault(HeaderNames.Date);

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

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            if (!HmacSha256Validator.IsSignatureValid(request.Method, request.GetDisplayUrl(), date, requestBody, receivedSignature, secret))
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
