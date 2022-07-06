using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Xml.Linq;
using System.Linq;

namespace CBRF_LX
{
    public class Program
    {
        private static readonly HttpClient client = new HttpClient();
        static ITelegramBotClient botClient = new TelegramBotClient("5304760086:AAEYbCPjgNzefw4AU3gc5FrihHukgj-ieu4");
        public class ResponseMessageData
        {
            public string Valute { get; set; }
        }

        private static async Task<string> PostRequestAsync(string url, string json)
        {
            using HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> GetRequestAsync(string url)
        {
            using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text != null)
                {
                    try
                    {
                        WebClient client = new WebClient();
                        var jsn = client.DownloadString("https://www.cbr-xml-daily.ru/daily_json.js");
                        JsonDocument jdoc = JsonDocument.Parse(jsn);
                        var dollar_value = jdoc.RootElement.GetProperty("Valute").GetProperty("USD").GetProperty("Value");
                        var dollar_previous = jdoc.RootElement.GetProperty("Valute").GetProperty("USD").GetProperty("Previous");
                        var euro_value = jdoc.RootElement.GetProperty("Valute").GetProperty("EUR").GetProperty("Value");
                        var euro_previous = jdoc.RootElement.GetProperty("Valute").GetProperty("EUR").GetProperty("Previous");
                        var cny_value = jdoc.RootElement.GetProperty("Valute").GetProperty("CNY").GetProperty("Value");
                        var cny_previous = jdoc.RootElement.GetProperty("Valute").GetProperty("CNY").GetProperty("Previous");
                        double cny_value_double = Math.Round((double)cny_value.GetDouble() / 10, 4);
                        double cny_previous_double = Math.Round((double)cny_previous.GetDouble() / 10, 4);
                        string cny_value_string = "" + cny_value_double;
                        cny_value_string = cny_value_string.Replace(',', '.');
                        string cny_previous_string = "" + cny_previous_double;
                        cny_previous_string = cny_previous_string.Replace(',', '.');

                        DateTime thisDay = DateTime.Today;
                        thisDay = thisDay.AddDays(-1);
                        string thisDay_str = thisDay.ToShortDateString().ToString();
                        thisDay_str = thisDay_str.Replace('.', '/');
                        var xml = client.DownloadString("https://www.cbr.ru/scripts/xml_metall.asp?date_req1=" + thisDay_str + "&date_req2=" + thisDay_str);
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Element("Metall").Elements("Record");
                        string au_buy = el.Where(x => x.Attribute("Code").Value == "1").Select(x => x.Element("Buy").Value).FirstOrDefault();
                        string au_sell = el.Where(x => x.Attribute("Code").Value == "1").Select(x => x.Element("Sell").Value).FirstOrDefault();
                        string ag_buy = el.Where(x => x.Attribute("Code").Value == "2").Select(x => x.Element("Buy").Value).FirstOrDefault();
                        string ag_sell = el.Where(x => x.Attribute("Code").Value == "2").Select(x => x.Element("Sell").Value).FirstOrDefault();
                        string pd_buy = el.Where(x => x.Attribute("Code").Value == "4").Select(x => x.Element("Buy").Value).FirstOrDefault();
                        string pd_sell = el.Where(x => x.Attribute("Code").Value == "4").Select(x => x.Element("Sell").Value).FirstOrDefault();
                        while (au_buy == null && au_sell == null && ag_buy == null && ag_sell == null && pd_buy == null && pd_sell == null)
                        {
                            thisDay = thisDay.AddDays(-1);
                            thisDay_str = thisDay.ToShortDateString().ToString();
                            thisDay_str = thisDay_str.Replace('.', '/');
                            xml = client.DownloadString("https://www.cbr.ru/scripts/xml_metall.asp?date_req1=" + thisDay_str + "&date_req2=" + thisDay_str);
                            xdoc = XDocument.Parse(xml);
                            el = xdoc.Element("Metall").Elements("Record");
                            au_buy = el.Where(x => x.Attribute("Code").Value == "1").Select(x => x.Element("Buy").Value).FirstOrDefault();
                            au_sell = el.Where(x => x.Attribute("Code").Value == "1").Select(x => x.Element("Sell").Value).FirstOrDefault();
                            ag_buy = el.Where(x => x.Attribute("Code").Value == "2").Select(x => x.Element("Buy").Value).FirstOrDefault();
                            ag_sell = el.Where(x => x.Attribute("Code").Value == "2").Select(x => x.Element("Sell").Value).FirstOrDefault();
                            pd_buy = el.Where(x => x.Attribute("Code").Value == "4").Select(x => x.Element("Buy").Value).FirstOrDefault();
                            pd_sell = el.Where(x => x.Attribute("Code").Value == "4").Select(x => x.Element("Sell").Value).FirstOrDefault();
                        }


                        await botClient.SendTextMessageAsync(message.Chat, $"Котировки валют (покупка|продажа):\nДоллар   |   {dollar_value} ₽   |   {dollar_previous} ₽\nЕвро        |   {euro_value} ₽   |   {euro_previous} ₽\nЮань      |   {cny_value_string} ₽     |   {cny_previous_string} ₽\n---------------------------------------------------------------\nКотировки драгмет. (покупка|продажа):\nЗолото        |   {au_buy} ₽/г    |   {au_sell} ₽/г\nСеребро     |   {ag_buy} ₽/г       |   {ag_sell} ₽/г\nПалладий   |   {pd_buy} ₽/г   |   {pd_sell} ₽/г");

                        Console.WriteLine("Успешно");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        await botClient.SendTextMessageAsync(message.Chat, "Ответ от сервиса не получен");
                    }
                }
                else
                    await botClient.SendTextMessageAsync(message.Chat, "Неизвестная ошибка");
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.WriteLine("====================================");
            Console.ReadLine();
        }
    }
}
