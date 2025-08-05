using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Compartilhado.Model;
using Newtonsoft.Json;
using static Amazon.Lambda.SNSEvents.SNSEvent;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Notificador;

public class Function
{
    private static IAmazonSimpleEmailService sesClient = new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.USEast1);

    public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
    {

        foreach (var record in snsEvent.Records)
        {
            var snsMessage = record.Sns.Message;
            var snsTopicArn = record.Sns.TopicArn;

            context.Logger.LogLine($"Recebendo mensagem do tópico: {snsTopicArn}");
            context.Logger.LogLine($"Mensagem: {snsMessage}");

            if (snsTopicArn.Contains("falha_coletor"))
            {
                context.Logger.LogLine($"Processando mensagem do Tipo FalhaColetor: {snsMessage}");
                if (IsValidJson(snsMessage))
                {
                    context.Logger.LogLine($"IsValidJson Processando mensagem do Tipo FalhaColetor");
                    var textEmail = JsonConvert.DeserializeObject<Pedido>(snsMessage);
                    var textContent = $"Notificação do Pedido\nStatus: FalhaColetor\nId: {textEmail.Id}\nCliente: {textEmail.Cliente.Nome}\nValor Total: {textEmail.ValorTotal}";

                    var sendRequest = new SendEmailRequest
                    {
                        Source = "ticket@leoguaruleo.com.br",
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { textEmail.Cliente.Email }
                        },
                        Message = new Message
                        {
                            Subject = new Content("Notificação do Pedido"),
                            Body = new Body
                            {
                                Text = new Content(textContent)
                            }
                        }
                    };
                    var response = await sesClient.SendEmailAsync(sendRequest);
                    context.Logger.LogLine($"SendEmailAsync Processando mensagem do Tipo FalhaColetor");
                }
                else
                {
                    context.Logger.LogLine($"NotIsValidJson Processando mensagem do Tipo FalhaColetor");
                }
            }
            else if (snsTopicArn.Contains("falha_pagador"))
            {
                context.Logger.LogLine($"Processando mensagem do Tipo FalhaPagador: {snsMessage}");
                if (IsValidJson(snsMessage))
                {
                    context.Logger.LogLine($"IsValidJson Processando mensagem do Tipo FalhaPagador");
                    var textEmail = JsonConvert.DeserializeObject<Pedido>(snsMessage);
                    var textContent = $"Notificação do Pedido\nStatus: Falha de Pagamento\nId: {textEmail.Id}\nCliente: {textEmail.Cliente.Nome}\nValor Total: {textEmail.ValorTotal}";

                    var sendRequest = new SendEmailRequest
                    {
                        Source = "ticket@leoguaruleo.com.br",
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { textEmail.Cliente.Email }
                        },
                        Message = new Message
                        {
                            Subject = new Content("Notificação do Pedido"),
                            Body = new Body
                            {
                                Text = new Content(textContent)
                            }
                        }
                    };
                    var response = await sesClient.SendEmailAsync(sendRequest);
                    context.Logger.LogLine($"SendEmailAsync Processando mensagem do Tipo FalhaPagador");
                }
                else
                {
                    context.Logger.LogLine($"NotIsValidJson Processando mensagem do Tipo FalhaPagador");
                }
            }
            else if (snsTopicArn.Contains("falha_faturador"))
            {
                context.Logger.LogLine($"Processando mensagem do Tipo FalhaFaturado: {snsMessage}");
                if (IsValidJson(snsMessage))
                {
                    context.Logger.LogLine($"IsValidJson Processando mensagem do Tipo FalhaFaturado");
                    var textEmail = JsonConvert.DeserializeObject<Pedido>(snsMessage);
                    var textContent = $"Notificação do Pedido\nStatus: FalhaFaturado\nId: {textEmail.Id}\nCliente: {textEmail.Cliente.Nome}\nValor Total: {textEmail.ValorTotal}";

                    var sendRequest = new SendEmailRequest
                    {
                        Source = "ticket@leoguaruleo.com.br",
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { textEmail.Cliente.Email }
                        },
                        Message = new Message
                        {
                            Subject = new Content("Notificação do Pedido"),
                            Body = new Body
                            {
                                Text = new Content(textContent)
                            }
                        }
                    };
                    var response = await sesClient.SendEmailAsync(sendRequest);
                    context.Logger.LogLine($"SendEmailAsync Processando mensagem do Tipo FalhaFaturado");
                }
                else
                {
                    context.Logger.LogLine($"NotIsValidJson Processando mensagem do Tipo FalhaReservador");
                }
            }
            else if (snsTopicArn.Contains("falha_reservador"))
            {
                context.Logger.LogLine($"Processando mensagem do Tipo FalhaReservador: {snsMessage}");
            }
            else if (snsTopicArn.Contains("faturado"))
            {
                context.Logger.LogLine($"Processando mensagem do Tipo TipoFaturado: {snsMessage}");
                if (IsValidJson(snsMessage))
                {
                    context.Logger.LogLine($"IsValidJson Processando mensagem do Tipo TipoFaturado");
                    var textEmail = JsonConvert.DeserializeObject<Pedido>(snsMessage);
                    var textContent = $"Notificação do Pedido\nStatus: Faturado\nId: {textEmail.Id}\nCliente: {textEmail.Cliente.Nome}\nValor Total: {textEmail.ValorTotal}";

                    var sendRequest = new SendEmailRequest
                    {
                        Source = "ticket@leoguaruleo.com.br",
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { textEmail.Cliente.Email }
                        },
                        Message = new Message
                        {
                            Subject = new Content("Notificação do Pedido"),
                            Body = new Body
                            {
                                Text = new Content(textContent)
                            }
                        }
                    };
                    var response = await sesClient.SendEmailAsync(sendRequest);
                    context.Logger.LogLine($"SendEmailAsync Processando mensagem do Tipo TipoFaturado");
                }
                else
                {
                    context.Logger.LogLine($"NotIsValidJson Processando mensagem do Tipo TipoFaturado");
                }
            }
            else
            {
                context.Logger.LogLine("Tópico não reconhecido. Ignorando a mensagem.");
            }
        }
    }

    private bool IsValidJson(string value)
    {
        value = value.Trim();
        return value.StartsWith("{") && value.EndsWith("}") || // Início e fim de um objeto JSON
               value.StartsWith("[") && value.EndsWith("]");  // Ou início e fim de um array JSON
    }
}