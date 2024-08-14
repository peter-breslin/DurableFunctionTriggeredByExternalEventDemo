using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Text.Json;

namespace DurableFunctionTriggeredByExternalEventDemo
{
    public static class ApprovalWorkflow
    {
        [FunctionName("ApprovalWorkflow")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await context.CallActivityAsync(nameof(RequestingApproval), string.Empty);

            using (var timoutCts = new CancellationTokenSource())
            {
                DateTime dueTime = context.CurrentUtcDateTime.AddMinutes(2);
                Task durableTimout = context.CreateTimer(dueTime, timoutCts.Token);

                Task<ApprovalDTO> approvalEvent = context.WaitForExternalEvent<ApprovalDTO>("ApprovalEvent");
                if (approvalEvent == await Task.WhenAny(approvalEvent, durableTimout))
                {
                    timoutCts.Cancel();
                    await context.CallActivityAsync("ProcessApproval", approvalEvent.Result);
                }
                else
                {
                    await context.CallActivityAsync("Escalate", string.Empty);
                }
            }
        }

    

        [FunctionName(nameof(RequestingApproval))]
        public static void RequestingApproval([ActivityTrigger] string text, ILogger log)
        {
            log.LogInformation("Requesting Approval");
        }

        [FunctionName(nameof(ProcessApproval))]
        public static void ProcessApproval([ActivityTrigger] ApprovalDTO dto, ILogger log)
        {
            log.LogInformation("ProcessApproval Approval");
        }

        [FunctionName(nameof(Escalate))]
        public static void Escalate([ActivityTrigger] string text, ILogger log)
        {
            log.LogInformation("Escalate");
        }


        [FunctionName("ApprovalWorkflow_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ApprovalWorkflow", null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(ApprovalWorkflowEndpoint))]
        public static async Task ApprovalWorkflowEndpoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {

            string content = await req.Content.ReadAsStringAsync();

            ApprovalDTO approvalDTO = JsonSerializer.Deserialize<ApprovalDTO>(content);

            log.LogInformation($"Started orchestration with ID = '{approvalDTO.InstanceId}'");

            await starter.RaiseEventAsync(approvalDTO.InstanceId, "ApprovalEvent", approvalDTO);
        }
    }
}