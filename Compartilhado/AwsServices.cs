using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
namespace Compartilhado;

public static class AwsServices
{
    private static readonly AmazonSQSClient sqsClient = new AmazonSQSClient(RegionEndpoint.USEast1);
  
    private static readonly AmazonDynamoDBClient dynamoDbClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
    private static readonly AmazonSimpleNotificationServiceClient snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USEast1); // Altere para sua região
    public static async Task SendMessageToSQS(string queueUrl, string message)
    {
        try
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = message
            };

            var response = await sqsClient.SendMessageAsync(sendMessageRequest);
            Console.WriteLine($"Mensagem enviada para SQS. MessageId: {response.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem para SQS: {ex.Message}");
        }
    }

    // SNS - Publicar Mensagem
    public static async Task PublishMessageToSNS(string topicArn, string message)
    {
        try
        {
            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message
            };
            var response = await snsClient.PublishAsync(publishRequest);
            Console.WriteLine($"Mensagem publicada no SNS. MessageId");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao publicar mensagem no SNS: {ex.Message}");
        }
    }

    // DynamoDB - Inserir Item
    public static async Task InsertItemToDynamoDB(string tableName, Dictionary<string, AttributeValue> item)
    {
        try
        {
            var putItemRequest = new PutItemRequest
            {
                TableName = tableName,
                Item = item
            };

            var response = await dynamoDbClient.PutItemAsync(putItemRequest);
            Console.WriteLine("Item inserido com sucesso no DynamoDB.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir item no DynamoDB: {ex.Message}");
        }
    }
}

