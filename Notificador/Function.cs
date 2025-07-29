using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.Pinpoint.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Notificador;

public class Function
{
    
    private static readonly string snsTopicArn = $"arn:aws:sns:us-east-1:676206946905:faturado";
    private readonly IAmazonSimpleNotificationService snsClient;


    public Function()
    {
        snsClient = new AmazonSimpleNotificationServiceClient();
      
    }
    public async Task HandleSNSMessageAsync(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (sqsEvent.Records.Count > 1)
            throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");

        var message = sqsEvent.Records.FirstOrDefault();
       
            context.Logger.LogLine($"----------Mensagem SNS recebida----------: {message.Body}");

            // Enviar notificação SNS (caso queira notificar um outro tópico SNS)
            await PublishToSNS(message.Body);

    }
 

    private async Task PublishToSNS(string message)
    {
        var publishRequest = new PublishRequest
        {
            TopicArn = snsTopicArn,
            Message = message,
            Subject = "-------Notificação SNS----------"
        };

        try
        {
            await snsClient.PublishAsync(publishRequest);
            Console.WriteLine("Notificação SNS enviada com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar SNS: {ex.Message}");
        }
    }

}