namespace tph.OrderRouter.Bittrex.ServiceLayer
{
    public class OrdersClient:BaseClient
    {
        #region Constructors

        public OrdersClient(string pURL,string pKey, string pSecret)
        {
            BaseURL = pURL;

            ApiKey = pKey;

            ApiSecret = pSecret;
        }

        #endregion
    }
}