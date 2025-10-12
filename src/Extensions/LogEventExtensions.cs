using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog.Events;
using Serilog.Parsing;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Seq.App.TelegramAlerts.Extensions
{
    public static class LogEventDataExtensions
    {
        public static LogEvent ToLogEvent(this Event<LogEventData> evt, string baseUrl, IDictionary<string, object> dynamicProps)
        {
            LogEventData data = evt.Data;
            List<LogEventProperty> properties = data.Properties.Select((KeyValuePair<string, object> p) => new LogEventProperty(p.Key, PropertyValueConverter.ToLogEventPropertyValue(p.Value))).ToList();
            
            if (dynamicProps != null && dynamicProps.Any())
            {
                foreach (KeyValuePair<string, object> kv in dynamicProps)
                {
                    properties.Add(new LogEventProperty(kv.Key, new ScalarValue(kv.Value)));
                }
            }

            return new LogEvent(
                data.LocalTimestamp, 
                (Serilog.Events.LogEventLevel)data.Level, 
                messageTemplate: new MessageTemplate(data.MessageTemplate, new MessageTemplateToken[0]), 
                exception: (data.Exception != null) ? new Exception(data.Exception) : null, properties: properties
            );
        }

        public static class PropertyValueConverter
        {
            public static LogEventPropertyValue ToLogEventPropertyValue(object value)
            {
                if (value is ScalarValue sv)
                {
                    return sv;
                }
                return new ScalarValue(value);
            }
        }
    }
}