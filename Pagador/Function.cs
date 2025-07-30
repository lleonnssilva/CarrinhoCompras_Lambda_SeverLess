using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;


// Use a camada do AWS Lambda
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Pagador
{
    public class Function
    {
        public AmazonDynamoDBClient AmazonDynamoDBClient { get; }
        public Function()
        {
            AmazonDynamoDBClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast1);
        }
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if (evnt.Records.Count > 1)
                throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");

            var message = evnt.Records.FirstOrDefault();

            if (message == null) return;
            await ProcessMessageAsync(message, context);
        }
        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            var pedido = JsonConvert.DeserializeObject<Pedido>(message.Body);
            try
            {
                bool isPaymentSuccessful = ProcessarPagamento(message.Body);
                if (isPaymentSuccessful)
                {
                    pedido.Status = StatusDoPedido.Pago;
                    await AmazonUtil.EnviarParaFila(FilaSQS.pago, pedido, $"Processo de pagamento feito com sucesso: {message.Body}");
                    await pedido.SalvarAsync();
                    LambdaLogger.Log($"Processo de pagamento feito com sucesso: {message.Body}");
                }
                else
                {
                    pedido.Status = StatusDoPedido.Reservado;
                    await AmazonUtil.EnviarParaFila(FilaSNS.falha, pedido, $"Falha no processamento do pagamento: {message.Body}");
                    LambdaLogger.Log($"Falha no processamento do pagamento: {message.Body}");
                }
            }
            catch (ConditionalCheckFailedException ex)
            {
                pedido.JustificativaDeCancelamento = $"Erro no pagamento!";
                pedido.Cancelado = false;
                context.Logger.LogInformation($"Erro:{pedido.JustificativaDeCancelamento}");
                await pedido.SalvarAsync();
            }
        }

        private bool ProcessarPagamento(string pedido)
        {
            try
            {

                Random random = new Random();
                bool sucessoPagamanto = random.Next(0, 2) == 0;
                if (!sucessoPagamanto)
                {
                    LambdaLogger.Log($"Erro ao processar pagamento: {pedido}");
                    return false; // Simula um erro no pagamento
                }
                LambdaLogger.Log($"Sucesso ao processar pagamento: {pedido}");
                return true; // Simula um pagamento bem-sucedido
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Erro ao processar pagamento: {ex.Message}");
                return false;
            }
        }

    }
}
