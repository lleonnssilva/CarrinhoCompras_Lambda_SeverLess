using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Compartilhado;
using Compartilhado.Model;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Estornador;

public class Function
{
    public AmazonDynamoDBClient AmazonDynamoDBClient { get; }
    public Function()
    {
        AmazonDynamoDBClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast1);
    }


    public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
    {
        context.Logger.LogLine($"Estornador Total de mensagem do tópico: {snsEvent.Records.Count}");
        foreach (var record in snsEvent.Records)
        {
            var snsMessage = record.Sns.Message;
            var snsTopicArn = record.Sns.TopicArn;
            context.Logger.LogLine($"Estornador Recebendo mensagem do tópico: {snsTopicArn}");
            context.Logger.LogLine($"Estornador Mensagem: {snsMessage}");

            if (snsTopicArn.Contains("falha_pagador"))
            {
                if (IsValidJson(snsMessage))
                {
                    context.Logger.LogLine($"IsValidJson Processando mensagem do Tipo Estornador");
                    var pedido = JsonConvert.DeserializeObject<Pedido>(snsMessage);

                    foreach (var produto in pedido.Produtos)
                    {
                        if (produto.Reservado)
                        {
                            await DevolverAoEstoque(produto.Id, produto.Quantidade);
                            produto.Reservado = false;
                            context.Logger.LogInformation($"Estornador: Produto devolvido ao estoque {produto.Id} - {produto.Nome}");
                        }


                    }

                    await pedido.SalvarAsync();
                    context.Logger.LogInformation($"Estornador: Pedido Falho : ID: {pedido.Id} - Status: {pedido.Status}");
                }
                else
                {
                    context.Logger.LogLine($"NotIsValidJson Processando mensagem do Tipo Estornador");
                }
            }
            else
            {
                context.Logger.LogLine($"Estornador nemuma mensagem");
            }
        }
    }
    private bool IsValidJson(string value)
    {
        value = value.Trim();
        return value.StartsWith("{") && value.EndsWith("}") || // Início e fim de um objeto JSON
               value.StartsWith("[") && value.EndsWith("]");  // Ou início e fim de um array JSON
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