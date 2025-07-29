


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
            

            // ARN do tópico SNS para o qual você deseja enviar a mensagem
            //string topicArn = "arn:aws:sns:us-east-1:123456789012:meu-topico";

            // Mensagem a ser enviada
            string mensagem = "Olá, esta é uma notificação do SNS!";

            // Criar a requisição de publicação
            var publishRequest = new PublishRequest
            {
                TopicArn = $"arn:aws:sns:us-east-1:676206946905:{fila}",
                Message = mensagem,   // Mensagem que será enviada
                Subject = "Notificação SNS"  // Assunto da mensagem
            };

            try
            {
                // Enviar a mensagem para o tópico SNS
                var response = _snsClient.PublishAsync(publishRequest).Result;

                // Exibir resposta
                Console.WriteLine("Mensagem enviada com sucesso!");
                Console.WriteLine("MessageId: " + response.MessageId);
            }
            catch (Exception ex)
            {
                // Tratar possíveis erros
                Console.WriteLine("Erro ao enviar a mensagem: " + ex.Message);
            }


            //var snsRequest = new PublishRequest
            //{
            //    TopicArn = $"arn:aws:sns:us-east-1:676206946905:{fila}",
            //    Message = msg,
            //};

            //try
            //{

            //    // Enviando a mensagem para o SNS
            //    var response = await _snsClient.PublishAsync(snsRequest);

            //    // Log de sucesso
            //    LambdaLogger.Log($"Mensagem enviada com sucesso para o SNS. ID da mensagem: {response.MessageId}\n- Msg:{msg}");
            //}
            //catch (Exception ex)
            //{
            //    // Log de erro com stack trace detalhado
            //    LambdaLogger.Log($"Erro ao enviar mensagem para o SNS: {ex.Message}\n{ex.StackTrace}");
            //}
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
