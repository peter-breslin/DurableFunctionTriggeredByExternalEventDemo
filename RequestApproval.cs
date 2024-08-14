using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DurableFunctionTriggeredByExternalEventDemo
{
    public static class RequestApproval
    {
        [FunctionName("RequestApproval")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var approved = await context.WaitForExternalEvent<bool>("RequestApprovalEvent");
            if (approved)
            {
                await context.CallActivityAsync(nameof(PrintMessage), "You said Yes!");
            }
            else
            {
                await context.CallActivityAsync(nameof(PrintMessage), "You said No");
            }
        }

        [FunctionName(nameof(PrintMessage))]
        public static void PrintMessage([ActivityTrigger] string message, ILogger log)
        {
            log.LogInformation(message);
        }

        [FunctionName("RequestApproval_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("RequestApproval", null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RequestApprovalEndpoint))]
        public static async Task RequestApprovalEndpoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
            {

                string content = await req.Content.ReadAsStringAsync();

                ApprovalDTO approvalDTO = JsonSerializer.Deserialize<ApprovalDTO>(content);

                log.LogInformation($"Started orchestration with ID = '{approvalDTO.InstanceId}'");

                await starter.RaiseEventAsync(approvalDTO.InstanceId, "RequestApprovalEvent", approvalDTO.IsApproved);
            }
        }
}