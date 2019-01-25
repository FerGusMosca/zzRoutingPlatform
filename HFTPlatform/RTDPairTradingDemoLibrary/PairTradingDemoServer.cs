using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using RTDPairTradingDemoLibrary.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace RTDPairTradingDemoLibrary
{

    [ComVisible(true)]
    [Guid("EBD9B4A9-3E17-45F0-A1C9-E134043923D3")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("RtdServer.PairTradingDemoServer")]
    public class PairTradingDemoServer : IRtdServer
    {
        #region IRtdServer attributes

        private IRTDUpdateEvent _callback;

        private System.Timers.Timer _timer;

        #endregion

        #region Private Attributes

        private List<int> Topics = new List<int>();

        private Dictionary<int, PairTradingRequest> RequestByTopic = new Dictionary<int, PairTradingRequest>();

        private Dictionary<int, string> TopicLatestData = new Dictionary<int, string>();

        private string RTDPositionRequestURL = "http://localhost:3038/api/PairTradingDemo/RequestPairTradingPosition";

        private string RTDStatusRequestURL = "http://localhost:3038/api/PairTradingDemo/RequestPairTradingStatus";


        #endregion

        #region Private Static Attributes

        private static object tLock = new object();

        private static int _TIMER_INTERVAL_MILISECONDS = 1000;//1 seconds

        private static int _TIMEOUT_REST_REQUEST_MILISECONDS = 100;//100 milisegundos

        #endregion

        #region Private Methods

        private void ExtractParameters(int TopicID, ref Array Strings)
        {
            if (Strings.Length < 6)
                throw new Exception(string.Format("Parámetros inválios para el tópico {0}", TopicID));

            PairTradingRequest ptReq = new PairTradingRequest()
            {
                LongSymbol = (string)Strings.GetValue(0),
                ShortSymbol = (string)Strings.GetValue(1),
                ConvertionRatio = Convert.ToDecimal(Strings.GetValue(2)),
                PlusCash =  Convert.ToDecimal(Strings.GetValue(3)),
                SpreadLong =  Convert.ToDecimal(Strings.GetValue(4)),
                QtyLong =  Convert.ToInt32(Strings.GetValue(5)),
                SpreadUnwind = (Strings.GetValue(6).ToString() != "" ? (decimal?) Convert.ToDecimal(Strings.GetValue(6)) : null),
                MaxUnhedgedAmmount = (Strings.GetValue(7).ToString() != "" ? (int?)Convert.ToInt32(Strings.GetValue(7)) : null),
                InitiateFirst = (Strings.GetValue(8).ToString() != "" ? (string) Strings.GetValue(8).ToString() : null),
                Account = (Strings.GetValue(9).ToString() != "" ? (string)Strings.GetValue(9).ToString() : null),
                Broker = (Strings.GetValue(10).ToString() != "" ? (string)Strings.GetValue(10).ToString() : null),
            };

            if(!RequestByTopic.ContainsKey(TopicID))
                RequestByTopic.Add(TopicID, ptReq);
        }

        private string DoGet(int topicId, string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.ReadWriteTimeout = _TIMEOUT_REST_REQUEST_MILISECONDS;

            request.ContentType = @"application/json";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                long length = response.ContentLength;
                var rawJson = new StreamReader(response.GetResponseStream()).ReadToEnd();

                RTDGatewayResponse resp = JsonConvert.DeserializeObject<RTDGatewayResponse>(rawJson); ;

                if (resp.IsOK)
                    return resp.Response;
                else
                    return resp.Error;
            }
        }

        private string ConstructPositionRequestURL(int topicId, PairTradingRequest ptReq)
        {
            
            string finalURL = string.Format("{0}?id={7}&longSymbol={1}&shortSymbol={2}&convertionRatio={3}&cash={4}&spreadLong={5}&qtyLong={6}",
                                            RTDPositionRequestURL, ptReq.LongSymbol,ptReq.ShortSymbol,ptReq.ConvertionRatio,ptReq.PlusCash,
                                            ptReq.SpreadLong, ptReq.QtyLong, topicId);

            if (ptReq.SpreadUnwind.HasValue)
                finalURL += string.Format("&spreadUnwind={0}", ptReq.SpreadUnwind.Value);

            if (ptReq.MaxUnhedgedAmmount.HasValue)
                finalURL += string.Format("&maxUnhedgedAmt={0}", ptReq.MaxUnhedgedAmmount.Value);

            if (!string.IsNullOrEmpty(ptReq.InitiateFirst))
                finalURL += string.Format("&initiateFirst={0}", ptReq.InitiateFirst);

            if (!string.IsNullOrEmpty(ptReq.Broker))
                finalURL += string.Format("&broker={0}", ptReq.Broker);

            if (!string.IsNullOrEmpty(ptReq.Account))
                finalURL += string.Format("&account={0}", ptReq.Account);

            return finalURL;
        }


        private string ConstructStatusRequestURL(int topicId)
        {
            string finalURL = string.Format("{0}?id={1}", RTDStatusRequestURL,  topicId);

            return finalURL;
        }


        private string DoRequestPairTrading(int topicId, PairTradingRequest ptReq, ref bool success)
        {
          
            try
            {
                
                string finalURL = ConstructPositionRequestURL(topicId,ptReq);
                
                string resp = DoGet(topicId, finalURL);

                success = true;

                return resp;
                

            }
            catch (Exception ex)
            {
                success = false;

                string errorMsg = string.Format("Error: {0}", ex.Message);

                return errorMsg;
            }
        }


        private string DoRequestPairTradingStatus(int topicId)
        {
            try
            {
                string finalURL = ConstructStatusRequestURL(topicId);
              
                string resp = DoGet(topicId, finalURL);

                return resp;
               

            }
            catch (Exception ex)
            {

                string errorMsg = string.Format("Error: {0}", ex.Message);

                return errorMsg;
            }
        }


        private void RequestPairTrading(object param)
        {

            int topicId = (int)param;
            PairTradingRequest req = RequestByTopic[topicId];

            bool success = false;

            while (!success)
            {
                try
                {


                    Thread.Sleep(1000);
                    string resp = DoRequestPairTrading(topicId, req, ref success);
                    TopicLatestData[topicId] = resp;

                    if (success)
                    {
                        Thread ptErThread = new Thread(RequestExecutionReportStatus);
                        ptErThread.Start(topicId);
                    }
                }
                catch (Exception ex)
                {
                    TopicLatestData[topicId] = ex.Message;
                }
            }
        }

        private void RequestExecutionReportStatus(object param)
        {
            int topicId = (int)param;
            PairTradingRequest req = RequestByTopic[topicId];

            try
            {
                while (TopicLatestData.ContainsKey(topicId))
                {
                    string resp = DoRequestPairTradingStatus(topicId);
                    TopicLatestData[topicId] = resp;
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                TopicLatestData[topicId] = ex.Message;
            }

        }

        private bool LoadParameters()
        {
            return true;
        }

        private void TimerEventHandler(object sender, EventArgs args)
        {

            // UpdateNotify is called to inform Excel that new data are available

            // the timer is turned off so that if Excel is busy, the TimerEventHandler is not called repeatedly

            _timer.Stop();

            _callback.UpdateNotify();
        }

        #endregion

        #region IRtdServer Methods

        public dynamic ConnectData(int TopicID, ref Array Strings, ref bool GetNewValues)
        {
            lock (tLock)
            {
                string format = Strings.GetValue(0).ToString();
                if (!Topics.Contains(TopicID))
                {
                    Topics.Add(TopicID);
                    TopicLatestData.Add(TopicID, "no data");
                }

                _timer.Start();

                try
                {
                    
                    ExtractParameters(TopicID, ref Strings);
                  
                    Thread ptRqThread = new Thread(RequestPairTrading);
                    ptRqThread.Start(TopicID);

                   

                    return "-";
                }
                catch (Exception ex)
                {
                    TopicLatestData[TopicID] = ex.Message;
                    return ex.Message;
                }
            }
        }

        public void DisconnectData(int TopicID)
        {
            _timer.Stop();
        }

        public int Heartbeat()
        {
            return 1;
        }

        public Array RefreshData(ref int TopicCount)
        {
            object[,] data = new object[2, Topics.Count];

            lock (tLock)
            {
                int index = 0;
                foreach (int topicId in Topics)
                {
                    data[0, index] = topicId;

                    try
                    {
                        string resp = TopicLatestData[topicId];
                        data[1, index] = resp;
                        index++;

                    }
                    catch (Exception ex)
                    {
                        data[1, index] = ex.Message;
                    }
                    finally
                    {
                        TopicCount = Topics.Count;

                        _timer.Start();

                    }
                }
            }

            return data;
        }

        public int ServerStart(IRTDUpdateEvent CallbackObject)
        {
            if (LoadParameters())
            {
                _callback = CallbackObject;

                _timer = new System.Timers.Timer();

                _timer.Elapsed += new ElapsedEventHandler(TimerEventHandler);

                _timer.Interval = _TIMER_INTERVAL_MILISECONDS;

                return 1;
            }
            else
                return -1;
        }

        public void ServerTerminate()
        {
            if (null != _timer)
            {
                _timer.Dispose();
                _timer = null;
            }

            RequestByTopic.Clear();
            Topics.Clear();
            TopicLatestData.Clear();
        }

        #endregion
    }
}
