using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctions
{
    public class FanOutFanIn
    {
        [FunctionName(nameof(FanOutFanIn))]
        public static async Task<List<string>> FanOutFanInRunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Parallel calls

            var tasks = new List<Task<string>>
            {
                context.CallActivityAsync<string>(nameof(FanOutFanInSayHello), "Tokyo"),
                context.CallActivityAsync<string>(nameof(FanOutFanInSayHello), "Dhaka"),
                context.CallActivityAsync<string>(nameof(FanOutFanInSayHello), "London")
            };

            var outputs = await Task.WhenAll(tasks);

            return outputs.ToList();
        }

        [FunctionName(nameof(FanOutFanInSayHello))]
        public static async Task<string> FanOutFanInSayHello([ActivityTrigger] string name, ILogger log)
        {
            await Task.Delay(Random.Shared.Next(5000, 10000));
            log.LogDebug($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName(nameof(FanOutFanInHttpStart))]
        public static async Task<HttpResponseMessage> FanOutFanInHttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "FanOutFanIn")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(FanOutFanIn));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //Async HTTP APIs
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
