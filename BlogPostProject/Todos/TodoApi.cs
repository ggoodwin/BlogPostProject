using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlogPostProject.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlogPostProject.Todos
{
    class TodoApi
    {
        [FunctionName("GetTodos")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req,
            [CosmosDB(
                databaseName: "BlogPostProjectDB",
                collectionName: "Todos",
                ConnectionStringSetting = "DBConnectionString"
            )]
            IEnumerable<Todo> todoSet,
            ILogger log)
        {
            log.LogInformation("Data fetched from Todos");
            return new OkObjectResult(todoSet);
        }

        [FunctionName("PostTodo")]
        public static async Task<IActionResult> RunPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
            [CosmosDB(
                databaseName: "BlogPostProjectDB",
                collectionName: "Todos",
                ConnectionStringSetting = "DBConnectionString"
            )]
            DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Posting to Todos");
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var newDocument = JsonConvert.DeserializeObject<Todo>(content);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("BlogPostProjectDB", "Todos");
            var createdDocument = await client.CreateDocumentAsync(collectionUri, newDocument);

            return new OkObjectResult(createdDocument.Resource);
        }

        [FunctionName("PutTodo")]
        public static async Task<IActionResult> RunPut(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)]
            HttpRequest req,
            [CosmosDB(
                databaseName: "BlogPostProjectDB",
                collectionName: "Todos",
                ConnectionStringSetting = "DBConnectionString"
            )]
            DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Editing Todos");
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var documentUpdate = JsonConvert.DeserializeObject<Todo>(content);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("BlogPostProjectDB", "Todos");

            var feedOptions = new FeedOptions { EnableCrossPartitionQuery = true };
            var existingDocument = client.CreateDocumentQuery<Todo>(collectionUri, feedOptions)
                .Where(d => d.id == documentUpdate.id)
                .AsEnumerable().FirstOrDefault();

            if (existingDocument == null)
            {
                log.LogWarning($"Todo: {documentUpdate.id} not found.");
                return new BadRequestObjectResult($"Todo not found.");
            }

            var documentUri = UriFactory.CreateDocumentUri("BlogPostProjectDB", "Todos", documentUpdate.id);
            await client.ReplaceDocumentAsync(documentUri, documentUpdate);

            return new OkObjectResult(documentUpdate);
        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> RunDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)]
            HttpRequest req,
            [CosmosDB(
                databaseName: "BlogPostProjectDB",
                collectionName: "Todos",
                ConnectionStringSetting = "DBConnectionString"
            )]
            DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Editing Todo");
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var documentUpdate = JsonConvert.DeserializeObject<Todo>(content);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("BlogPostProjectDB", "Todos");

            var feedOptions = new FeedOptions { EnableCrossPartitionQuery = true };
            var existingDocument = client.CreateDocumentQuery<Todo>(collectionUri, feedOptions)
                .Where(d => d.id == documentUpdate.id)
                .AsEnumerable().FirstOrDefault();

            if (existingDocument == null)
            {
                log.LogWarning($"Todos: {documentUpdate.id} not found.");
                return new BadRequestObjectResult($"Todo not found.");
            }

            var documentUri = UriFactory.CreateDocumentUri("BlogPostProjectDB", "Todos", documentUpdate.id);
            await client.DeleteDocumentAsync(documentUri, new RequestOptions
            {
                PartitionKey = new PartitionKey(documentUpdate.id)
            });

            return new OkObjectResult(true);
        }
    }
}
