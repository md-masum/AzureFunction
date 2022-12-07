using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace DurableFunctions
{
    public class HumanInteraction
    {
        [FunctionName(nameof(HumanInteraction))]
        public static async Task HumanInteractionRunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await context.CallActivityAsync("RequestApproval", null);
            using var timeoutCts = new CancellationTokenSource();
            DateTime dueTime = context.CurrentUtcDateTime.AddHours(72);
            Task durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

            Task<bool> approvalEvent = context.WaitForExternalEvent<bool>("ApprovalEvent");
            if (approvalEvent == await Task.WhenAny(approvalEvent, durableTimeout))
            {
                timeoutCts.Cancel();
                await context.CallActivityAsync(nameof(ProcessApproval), approvalEvent.Result);
            }
            else
            {
                await context.CallActivityAsync("Escalate", null);
            }
        }

        [FunctionName(nameof(ProcessApproval))]
        public static async Task ProcessApproval([ActivityTrigger] string name, ILogger log)
        {
            await Task.Delay(Random.Shared.Next(5000, 10000));
            log.LogDebug($"Processing approval");
        }

        [FunctionName(nameof(HumanInteractionHttpStart))]
        public static async Task<HttpResponseMessage> HumanInteractionHttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "HumanInteraction")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(HumanInteraction));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //Async HTTP APIs
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
