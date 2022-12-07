using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctions
{
    public static class FunctionChaining
    {
        [FunctionName(nameof(FunctionChaining))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>
            {
                // Replace "hello" with the name of your Durable Activity Function.
                await context.CallActivityAsync<string>(nameof(FunctionChainingSayHello), "Tokyo"),
                await context.CallActivityAsync<string>(nameof(FunctionChainingSayHello), "Dhaka"),
                await context.CallActivityAsync<string>(nameof(FunctionChainingSayHello), "London")
            };

            // returns ["Hello Tokyo!", "Hello Dhaka!", "Hello London!"]
            return outputs;
        }

        [FunctionName(nameof(FunctionChainingSayHello))]
        public static string FunctionChainingSayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName(nameof(FunctionChainingHttpStart))]
        public static async Task<HttpResponseMessage> FunctionChainingHttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "FunctionChaining")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(FunctionChaining));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //Async HTTP APIs
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}