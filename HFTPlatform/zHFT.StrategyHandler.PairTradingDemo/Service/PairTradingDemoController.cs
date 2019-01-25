using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.PairTradingDemo.Common.DTO;

namespace zHFT.StrategyHandler.PairTradingDemo.Service
{
    public delegate PairTradingResponse OnProcessPairTradingRequest(PairTradingRequest rq);
    public delegate PairTradingResponse OnProcessPairTradingStatus(int id);
    public delegate void OnLog(string msg, Constants.MessageType type);

    public class PairTradingDemoController : ApiController
    {
        #region Public Static Attributes

        public static OnLog OnLog { get; set; }

        public static OnProcessPairTradingRequest OnProcessPairTradingRequest { get; set; }

        public static OnProcessPairTradingStatus OnProcessPairTradingStatus { get; set; }

        #endregion

        #region Public Methods

        [HttpGet]
        public HttpResponseMessage RequestPairTradingPosition()
        {
            return Request.CreateResponse(HttpStatusCode.OK,
                                                       new
                                                       {
                                                           IsOK = true,
                                                           Response = "Test"
                                                       });
        
        }

        [HttpGet]
        public HttpResponseMessage RequestPairTradingStatus(int id)
        {
            try
            {
                PairTradingResponse ptStatusResp = OnProcessPairTradingStatus(id);
                return Request.CreateResponse(HttpStatusCode.OK,
                                                    new
                                                    {
                                                        IsOK = true,
                                                        Response = ptStatusResp.Response
                                                    });
            
            }
            catch (Exception ex)
            {

                OnLog(string.Format("@PairTradingDemoController:Error receiving RequestPairTradingStatus  for id {0}:{1}",
                        id, ex.Message), Constants.MessageType.Error);


                return Request.CreateResponse(HttpStatusCode.OK,
                                                        new
                                                        {
                                                            IsOK = false,
                                                            Error = ex.Message,
                                                        });
            }

        
        }


        [HttpGet]
        public HttpResponseMessage RequestPairTradingPosition(int id,string longSymbol, string shortSymbol,decimal convertionRatio,decimal cash,decimal spreadLong,
                                                                int qtyLong, decimal? spreadUnwind = null, int? maxUnhedgedAmt = null, string initiateFirst = null,
                                                                string broker=null, string account=null)
        {

            PairTradingRequest req = new PairTradingRequest()
            {
                Id=id,
                LongSymbol = longSymbol,
                ShortSymbol = shortSymbol,
                ConvertionRatio = convertionRatio,
                PlusCash = cash,
                SpreadLong = spreadLong,
                SpreadUnwind = spreadUnwind,
                QtyLong = qtyLong,
                MaxUnhedgedAmmount = maxUnhedgedAmt,
                InitiateFirst = initiateFirst,
                Account = account,
                Broker = broker
            };

            try
            {

                OnLog(string.Format("@PairTradingDemoController:Received RequestPairTradingPosition for symbols: LONG {0}- SHORT {1}",
                                        req.LongSymbol, req.ShortSymbol),
                                        Constants.MessageType.Information);

                PairTradingResponse ptResp = OnProcessPairTradingRequest(req);
                return Request.CreateResponse(HttpStatusCode.OK,
                                                    new
                                                    {
                                                        IsOK = true,
                                                        Response = ptResp.Response
                                                    });
            }
            catch (Exception ex)
            {

                OnLog(string.Format("@PairTradingDemoController:Error receiving RequestPairTradingPosition  for symbols: LONG {0}- SHORT {1}:{2}",
                        req.LongSymbol, req.ShortSymbol, ex.Message),Constants.MessageType.Error);


                return Request.CreateResponse(HttpStatusCode.OK,
                                                        new
                                                        {
                                                            IsOK = false,
                                                            Error = ex.Message,
                                                        });
            }
        }


        #endregion

    }
}
