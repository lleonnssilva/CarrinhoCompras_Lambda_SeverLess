using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
using Newtonsoft.Json;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Reservador;

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
        pedido.Status = StatusDoPedido.Reservado;
        foreach (var produto in pedido.Produtos)
        {
            try
            {
                await BaixarEstoque(produto.Id, produto.Quantidade);
                produto.Reservado = true;
                context.Logger.LogInformation($"Produto baixado do estoque {produto.Id} - {produto.Nome}");
            }
            catch (ConditionalCheckFailedException ex)
            {
                pedido.JustificativaDeCancelamento = $"Produto indisponível no estoque {produto.Id} - {produto.Nome}";
                pedido.Cancelado = true;
                context.Logger.LogInformation($"Erro:{pedido.JustificativaDeCancelamento}");
                break;
            }
           
        }

        if (pedido.Cancelado)
        {
            foreach (var produto in pedido.Produtos)
            {
                if (produto.Reservado)
                {
                    await DevolverAoEstoque(produto.Id, produto.Quantidade);
                    produto.Reservado = false;
                    context.Logger.LogInformation($"Produto devolvido ao estoque {produto.Id} - {produto.Nome}");
                }
                    

            }

            var msg = $"Falha no processamento do pedido {pedido.Id}. " +
                              $"Pedido Cancelado: {(pedido.Cancelado ? "Sim" : "Não")}. " +
                              $"Obs: (Status: {pedido.Status} - Totl: {pedido.ValorTotal})" +
                              $"Cliente: {pedido.Cliente.Nome}. " +
                              $"Data: {pedido.DataDeCriacao.ToString("yyyy-MM-dd HH:mm:ss")}";
            context.Logger.LogInformation($"Pedido Falho : ID: {pedido.Id} - Status: {pedido.Status}");
            await AmazonUtil.EnviarParaFila(FilaSNS.falha, pedido, msg);
            await pedido.SalvarAsync();
        }
        else
        {
            context.Logger.LogInformation($"Pedido Reservado : ID: {pedido.Id} - Status: {pedido.Status}");
            await AmazonUtil.EnviarParaFila(FilaSQS.reservado, pedido, $"Pedido Reservado : ID: {pedido.Id} - Status: {pedido.Status}");
            await pedido.SalvarAsync();
        }

    }

    private async Task BaixarEstoque(string id, int quantidade)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                {"Id", new AttributeValue{ S=id } }
            },
            UpdateExpression = "SET Quantidade = (Quantidade - :quantidadeDoPedido)",
            ConditionExpression = "Quantidade >= :quantidadeDoPedido",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":quantidadeDoPedido", new AttributeValue { N=quantidade.ToString() } }
            }
        };
        await AmazonDynamoDBClient.UpdateItemAsync(request);
    }

    private async Task DevolverAoEstoque(string id, int quantidade)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                {"Id", new AttributeValue{ S=id } }
            },
            UpdateExpression = "SET Quantidade = (Quantidade + :quantidadeDoPedido)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":quantidadeDoPedido", new AttributeValue { N=quantidade.ToString() } }
            }
        };
        await AmazonDynamoDBClient.UpdateItemAsync(request);
    }
}