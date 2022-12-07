using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctions
{
    internal class Monitor
    {
        [FunctionName(nameof(Monitor))]
        public static async Task MonitorRunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            while (true)
            {
                var jobStatus = await context.CallActivityAsync<string>("GetJobStatus", 1);
                if (jobStatus == "Completed")
                {
                    // Perform an action when a condition is met.
                    await context.CallActivityAsync("SendAlert", 1);
                    break;
                }
                // Orchestration sleeps until this time.
                var nextCheck = context.CurrentUtcDateTime.AddSeconds(60);
                await context.CreateTimer(nextCheck, CancellationToken.None);
            }
        }

        [FunctionName(nameof(GetJobStatus))]
        public static async Task<string> GetJobStatus([ActivityTrigger] int jobId, ILogger log)
        {
            await Task.Delay(Random.Shared.Next(5000, 10000));
            log.LogDebug($"Status of job {jobId} is 'Complete'.");
            if(jobId == 3) return "Completed";
            return "Processing";
        }

        [FunctionName(nameof(SendAlert))]
        public static async Task<string> SendAlert([ActivityTrigger] int jobId, ILogger log)
        {
            await Task.Delay(Random.Shared.Next(5000, 10000));
            log.LogDebug($"Alert has been sent for job {jobId}.");
            return "Completed";
        }

        [FunctionName(nameof(MonitorHttpStart))]
        public static async Task<HttpResponseMessage> MonitorHttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Monitor")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(Monitor));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //Async HTTP APIs
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
