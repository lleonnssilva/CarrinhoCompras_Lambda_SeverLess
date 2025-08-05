using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;


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
            context.Logger.LogInformation($"Pagador SQSEvent: {evnt.Records.FirstOrDefault()}");
            if (evnt.Records.Count > 1)
                throw new InvalidOperationException("Pagador: Somente uma mensagem pode ser tratada por vez");

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
                    await pedido.SalvarAsync();
                    context.Logger.LogInformation($"Pagador: Processo de pagamento feito com sucesso: {message.Body}");
                    await AmazonUtil.EnviarParaFila(FilaSQS.pago, pedido, $"Pagador: Processo de pagamento feito com sucesso: {message.Body}");
                   
                    
                }
                else
                {
                    pedido.Status = StatusDoPedido.Reservado;
                    pedido.Cancelado = false;
                    pedido.JustificativaDeCancelamento = $"Pagador: Falha no processamento do pagamento: {message.Body}";
                    context.Logger.LogInformation($"Pagador: Falha no processamento do pagamento");
                    await pedido.SalvarAsync();
                    context.Logger.LogInformation($"Pagador: Falha no processamento do pagamento: {message.Body}");
                    await AmazonUtil.EnviarParaFila(FilaSNS.falha_pagador, pedido, message.Body);
                    
                }
            }
            catch (ConditionalCheckFailedException ex)
            {
                pedido.JustificativaDeCancelamento = $"Pagador: Erro no pagamento!";
                pedido.Cancelado = false;
                context.Logger.LogInformation($"Pagador: Erro:{pedido.JustificativaDeCancelamento}");
                await pedido.SalvarAsync();
            }
        }

        private bool ProcessarPagamento(string pedido)
        {
           
                Random random = new Random();
                bool sucessoPagamanto = random.Next(0, 2) == 0;
                if (!sucessoPagamanto)
                    return false; 
                return true;
           
        }

    }
}
