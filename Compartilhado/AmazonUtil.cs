


using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
namespace Compartilhado
{
    public static class AmazonUtil
    {
        private static readonly AmazonDynamoDBClient Client = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
        private static readonly DynamoDBContext Context = new DynamoDBContext(Client);
        private static readonly AmazonSimpleNotificationServiceClient _snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USEast1); 

        public static async Task SalvarAsync(this Pedido pedido)
        {

            await Context.SaveAsync(pedido);
        }

        public static T ToObject<T>(this Dictionary<string, AttributeValue> dictionary)
        {
            try
            {
               

                // Converte o dicionário para um documento
                var doc = Document.FromAttributeMap(dictionary);

                // Converte o documento para o tipo solicitado
                return Context.FromDocument<T>(doc);
            }
            catch (Exception ex)
            {
                // Handle errors (logging, rethrow, etc.)
                throw new InvalidOperationException("Error converting DynamoDB data to object", ex);
            }
        }

        public static async Task EnviarParaFila(FilaSQS fila, Pedido pedido, string msg)
        {
            var json  = JsonConvert.SerializeObject(pedido);
            var client = new AmazonSQSClient(RegionEndpoint.USEast1);
            var request = new SendMessageRequest
            {
                QueueUrl = $"https://sqs.us-east-1.amazonaws.com/676206946905/{fila}",
                MessageBody = json
            };

            await client.SendMessageAsync(request);
        }
        public static async Task EnviarParaFila(FilaSNS fila, Pedido pedido, string msg)
        {

            var publishRequest = new PublishRequest
            {
                TopicArn = $"arn:aws:sns:us-east-1:676206946905:{fila}",
                Message = JsonConvert.SerializeObject(pedido), 
                Subject = "Notificação SNS"
            };

            try
            {
                var response = _snsClient.PublishAsync(publishRequest).Result;
                Console.WriteLine($"Mensagem sucesso: Tipo:EnviarParaFila(FilaSNS), Id:{response.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mensagem erro: Tipo:EnviarParaFila(FilaSNS), msg:{ex.Message}");
            }

        }
        public static Amazon.DynamoDBv2.Model.AttributeValue ToAmazonAttributeValue(this DynamoDBEvent.AttributeValue attributeValue)
        {
            if (attributeValue.S != null)
                return new Amazon.DynamoDBv2.Model.AttributeValue { S = attributeValue.S };
            if (attributeValue.N != null)
                return new Amazon.DynamoDBv2.Model.AttributeValue { N = attributeValue.N };
            if (attributeValue.B != null)
                return new Amazon.DynamoDBv2.Model.AttributeValue { B = attributeValue.B };
            if (attributeValue.M != null)
                return new Amazon.DynamoDBv2.Model.AttributeValue { M = attributeValue.M.ToAmazonAttributeValues() }; // Recursão para Map
            if (attributeValue.L != null)
                return new Amazon.DynamoDBv2.Model.AttributeValue { L = attributeValue.L.Select(a => a.ToAmazonAttributeValue()).ToList() }; // Recursão para List
            return new Amazon.DynamoDBv2.Model.AttributeValue();
        }

        // Função para converter um Dictionary<string, DynamoDBEvent.AttributeValue> para Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        public static Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> ToAmazonAttributeValues(this Dictionary<string, DynamoDBEvent.AttributeValue> dictionary)
        {
            return dictionary.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToAmazonAttributeValue()
            );
        }
    }
}
