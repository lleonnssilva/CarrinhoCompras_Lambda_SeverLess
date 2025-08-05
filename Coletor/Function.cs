using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Compartilhado;
using Compartilhado.Enums;
using Compartilhado.Model;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Coletor;

public class Function
{
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        foreach (var record in dynamoEvent.Records)
        {
           
            if (record.EventName == "INSERT")
            {
                var pedidoDictionary = record.Dynamodb.NewImage.ToAmazonAttributeValues();
                var pedido = pedidoDictionary.ToObject<Pedido>();
                pedido.Status = StatusDoPedido.Coletado;

                try
                {
                    await ProcessarValorDoPedido(pedido);
                    context.Logger.LogLine("Coletor: Processar valor do pedido");

                    await AmazonUtil.EnviarParaFila(FilaSQS.pedido, pedido, $"Sucesso na coleta do pedido: '{pedido.Id}'");
                    
                }
                catch (Exception ex)
                {
                    pedido.JustificativaDeCancelamento = ex.Message;
                    pedido.Cancelado = true;
                    await AmazonUtil.EnviarParaFila(FilaSNS.falha_coletor, pedido, $"Coletor: Erro na coleta do pedido: '{ex.Message}'");
                }

                await pedido.SalvarAsync();
                context.Logger.LogLine($"Coletor: Sucesso na coleta do pedido: '{pedido.Id}'");
            }
        }
    }

    private async Task ProcessarValorDoPedido(Pedido pedido)
    {
        foreach (var produto in pedido.Produtos)
        {
            var produtoDoEstoque = await ObterProdutoDoDynamoDBAsync(produto.Id);
            if (produtoDoEstoque == null) throw new InvalidOperationException($"Produto não encontrado na tabela estoque. {produto.Id}");

            produto.Valor = produtoDoEstoque.Valor;
            produto.Nome = produtoDoEstoque.Nome;
        }

        var valorTotal = pedido.Produtos.Sum(x => x.Valor * x.Quantidade);
        if (pedido.ValorTotal != 0 && pedido.ValorTotal != valorTotal)
            throw new InvalidOperationException($"O valor esperado do pedido é de R$ {pedido.ValorTotal} e o valor verdadeiro é R$ {valorTotal}");

        pedido.ValorTotal = valorTotal;
    }

    private async Task<Produto> ObterProdutoDoDynamoDBAsync(string id)
    {
        var client = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
        var request = new QueryRequest
        {
            TableName = "Estoque",
            KeyConditionExpression = "Id = :v_id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":v_id", new AttributeValue { S = id } } }
        };

        var response = await client.QueryAsync(request);
        var item = response.Items.FirstOrDefault();
        if (item == null) return null;
        return item.ToObject<Produto>();
    }
}
