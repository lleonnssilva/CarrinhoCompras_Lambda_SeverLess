using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;


// Use a camada do AWS Lambda
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Faturador
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
                bool isFaturado= FaturarPagamento(message.Body);
                if (isFaturado)
                {
                  
                    await AmazonUtil.EnviarParaFila(FilaSNS.faturado, pedido, $"Processo de faturamento feito com sucesso: {message.Body}");
                    pedido.Status = StatusDoPedido.Faturado;
                    await pedido.SalvarAsync();
                    LambdaLogger.Log($"SalvarAsync Processo de faturamento feito com sucesso: {message.Body}");
                }
                else
                {
                    await AmazonUtil.EnviarParaFila(FilaSNS.falha, pedido, "Falha no faturamento do pagamento");
                    LambdaLogger.Log($"Falha no faturamento do pagamento: {message.Body}");
                }
            }
            catch (ConditionalCheckFailedException ex)
            {
                pedido.JustificativaDeCancelamento = $"Erro no faturamento!";
                context.Logger.LogInformation($"Erro:{pedido.JustificativaDeCancelamento}");
                await pedido.SalvarAsync();
            }
        }

        private bool FaturarPagamento(string pedido)
        {
            try
            {
                // Lógica para processar o pagamento (simulação aqui)
                // Você pode adicionar uma lógica real de pagamento como integração com bancos, gateways, etc.
                if (pedido.Contains("Erro"))
                {
                    LambdaLogger.Log($"Erro ao faturar o pagamento: {pedido}");
                    return false; // Simula um erro no pagamento
                }
                LambdaLogger.Log($"Sucesso ao faturar o pagamento: {pedido}");
                return true; // Simula um pagamento bem-sucedido
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Erro ao faturar pagamento: {ex.Message}");
                return false;
            }
        }

    }
}
