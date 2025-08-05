using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;


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
                throw new InvalidOperationException("Faturador: Somente uma mensagem pode ser tratada por vez");

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
                    pedido.Status = StatusDoPedido.Faturado;
                    await pedido.SalvarAsync();
                    context.Logger.LogInformation($"Faturador: Processo de faturamento feito com sucesso: {message.Body}");
                    await AmazonUtil.EnviarParaFila(FilaSNS.faturado, pedido, $"Faturador: Processo de faturamento feito com sucesso: {message.Body}");
                  
                }
                else
                {
                   
                    await AmazonUtil.EnviarParaFila(FilaSNS.falha_faturador, pedido, "Faturador: Falha no faturamento do pagamento");
                    context.Logger.LogInformation($"Faturador: Falha no faturamento do pagamento: {message.Body}");
                }
            }
            catch (ConditionalCheckFailedException ex)
            {
                pedido.JustificativaDeCancelamento = $"Faturador: Erro no faturamento!";
                context.Logger.LogInformation($"Faturador: Erro:{pedido.JustificativaDeCancelamento}");
                await pedido.SalvarAsync();
            }
        }

        private bool FaturarPagamento(string pedido)
        {
            Random random = new Random();
            bool sucessoPagamanto = random.Next(0, 2) == 0;
            if (!sucessoPagamanto)
                return false;
            return true;
        }

    }
}
