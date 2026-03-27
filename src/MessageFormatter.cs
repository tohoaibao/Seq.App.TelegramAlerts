using System.Collections.Generic;
using System.IO;
using Seq.App.TelegramNotifier.Extensions;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Seq.Syntax.Templates;
using Serilog.Events;

namespace Seq.App.TelegramNotifier
{
    public class MessageFormatter
    {
        private readonly string _baseUrl;

        private readonly string _messageTemplate;

        public MessageFormatter(string baseUrl, string messageTemplate)
        {
            _baseUrl = baseUrl;
            _messageTemplate = string.IsNullOrWhiteSpace(messageTemplate) ? "{RenderedMessage}" : messageTemplate;
        }

        public string GenerateMessageText(Event<LogEventData> evt)
        {
            Dictionary<string, object> dynamicProps = GetDynamicProperties(evt);
            LogEvent logEvent = evt.ToLogEvent(_baseUrl, dynamicProps);

            using StringWriter writer = new();

            var template = new ExpressionTemplate(_messageTemplate);
            template.Format(logEvent, writer);

            return writer.ToString();
        }

        private Dictionary<string, object> GetDynamicProperties(Event<LogEventData> evt)
        {
            return new Dictionary<string, object> { ["Link"] = $"[\ud83d\udd17]({_baseUrl.TrimEnd('/')}/#/events?filter=@Id%3D%3D'{evt.Id}'&show=expanded)" };
        }
    }
}
