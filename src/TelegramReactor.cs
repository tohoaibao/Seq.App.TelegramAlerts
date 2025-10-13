using MihaZupan;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Seq.App.TelegramAlerts
{
    [SeqApp("TelegramAlerts", Description = "An app that delivers alerts and log events to Telegram, supporting template syntax.")]
    public class TelegramReactor : SeqApp, ISubscribeToAsync<LogEventData>
    {
        private readonly Lazy<TelegramBotClient> _telegram;

        private readonly Throttling<uint> _throttling = new();

        [SeqAppSetting(DisplayName = "Bot authentication token", HelpText = "Refer to Telegram api documentation https://core.telegram.org/bots/api#authorizing-your-bot")]
        public string BotToken { get; set; }

        [SeqAppSetting(DisplayName = "Group chat identifier", HelpText = "Unique identifier for your group chat (include minus)")]
        public long ChatId { get; set; }

        [SeqAppSetting(DisplayName = "Seq Base URL", HelpText = "Used for generating permalinks to events in Telegram messages.", IsOptional = true)]
        public string BaseUrl { get; set; }

        [SeqAppSetting(HelpText = "The message template to use when writing the message to Telegram. Use Seq template syntax (e.g., {Level}, {@x}, {Substring(@x, 0, 200)})." +
            " The default is {RenderedMessage}." +
            " Refer: https://datalust.co/docs/template-syntax", InputType = SettingInputType.LongText, IsOptional = true)]
        public string MessageTemplate { get; set; }

        [SeqAppSetting(DisplayName = "Suppression time (minutes)", IsOptional = true, HelpText = "Once an event type has been sent to Telegram, the time to wait before sending again. The default is zero.")]
        public int SuppressionMinutes { get; set; }

        [SeqAppSetting(DisplayName = "Socks5 proxy host name", IsOptional = true)]
        public string Socks5ProxyHost { get; set; }

        [SeqAppSetting(DisplayName = "Socks5 proxy port", IsOptional = true)]
        public int Socks5ProxyPort { get; set; }

        [SeqAppSetting(DisplayName = "Socks5 proxy username", IsOptional = true)]
        public string Socks5ProxyUserName { get; set; }

        [SeqAppSetting(DisplayName = "Socks5 proxy password", IsOptional = true, InputType = SettingInputType.Password)]
        public string Socks5ProxyPassword { get; set; }

        public TelegramReactor()
        {
            _telegram = new Lazy<TelegramBotClient>(CreateTelegramBotClient, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private TelegramBotClient CreateTelegramBotClient()
        {
            if (string.IsNullOrEmpty(Socks5ProxyHost))
            {
                return new TelegramBotClient(BotToken);
            }
            HttpToSocks5Proxy proxy = (string.IsNullOrEmpty(Socks5ProxyUserName) ? new HttpToSocks5Proxy(Socks5ProxyHost, Socks5ProxyPort) : new HttpToSocks5Proxy(Socks5ProxyHost, Socks5ProxyPort, Socks5ProxyUserName, Socks5ProxyPassword));
            return new TelegramBotClient(BotToken, new HttpClient(new HttpClientHandler
            {
                Proxy = proxy
            }));
        }

        private string GetBaseUri()
        {
            return (BaseUrl ?? base.Host.BaseUri).TrimEnd('/');
        }

        public async Task OnAsync(Event<LogEventData> evt)
        {
            if (_throttling.TryBegin(evt.EventType, TimeSpan.FromMinutes(SuppressionMinutes)))
            {
                string message = new MessageFormatter(GetBaseUri(), MessageTemplate).GenerateMessageText(evt);
                await _telegram.Value.SendMessage(ChatId, message, ParseMode.Markdown);
            }
        }
    }
}
