using Amazon.SQS;
using Amazon.SQS.Model;

class Program
{
    private static string queueUrl = "https://sqs.us-east-1.amazonaws.com/676206946905/notificacao-consumidor1"; // Substitua pela URL da sua fila SQS

    //$"arn:aws:sns:us-east-1:676206946905:faturado"
    static async Task Main(string[] args)
    {

        await LoadSQS();


    }
    static async Task LoadSQS()
    {
        var sqsClient = new AmazonSQSClient(Amazon.RegionEndpoint.USEast1);
        Console.WriteLine("Iniciando o processo de escuta da fila SQS...");
        while (true)
        {
            try
            {
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 1,  
                    WaitTimeSeconds = 20, 
                    VisibilityTimeout = 30,   
                    MessageAttributeNames = new List<string> { "EventType" }
                };

                var receiveMessageResponse = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);

                if (
                    receiveMessageResponse.Messages != null &&
                    receiveMessageResponse.Messages.Count > 0)// &&
                                                              //receiveMessageResponse.Messages[0].MessageAttributes != null) 
                                                              //receiveMessageResponse.Messages[0].MessageAttributes.ContainsKey("EventType") &&
                                                              //receiveMessageResponse.Messages[0].MessageAttributes["EventType"].StringValue == "ClienteCadastradoEvent")
                {
                    var message = receiveMessageResponse.Messages[0];



                    Console.WriteLine($"Mensagem recebida: {message.Body}");

                    await ProcessMessageAsync(message);

                    var deleteMessageRequest = new DeleteMessageRequest
                    {
                        QueueUrl = queueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    };

                    await sqsClient.DeleteMessageAsync(deleteMessageRequest);


                }
                else
                {
                    Console.WriteLine("Nenhuma mensagem encontrada. Aguardando...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao tentar receber mensagens: {ex.Message}");
            }

            await Task.Delay(1000);
        }
    }
    private static async Task ProcessMessageAsync(Message message)
    {
        Console.WriteLine($"Processando mensagem: {message.Body}");
        await Task.Delay(500);
    }

    private static async Task DeleteMessageAsync(Message message)
    {
        var deleteMessageRequest = new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        };

        //await sqsClient.DeleteMessageAsync(deleteMessageRequest);
        Console.WriteLine("Mensagem excluída da fila.");
        await LoadSQS();
    }
}