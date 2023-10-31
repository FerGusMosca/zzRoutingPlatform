using Newtonsoft.Json.Converters;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Converters
{
    public class WebsocketConnectorFullTimeConverter : IsoDateTimeConverter
    {

        public WebsocketConnectorFullTimeConverter()
        {
            base.DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        }
    }
    
    public class WebsocketConnectorDateTimeConverter : IsoDateTimeConverter
    {

        public WebsocketConnectorDateTimeConverter()
        {
            base.DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        }
    }

}