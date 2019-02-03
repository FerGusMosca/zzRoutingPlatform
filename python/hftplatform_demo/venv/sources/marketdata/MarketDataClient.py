import sys
sys.path.insert(0, ".\\venv\\IBApis")
from ibapi.wrapper import EWrapper
from ibapi.client import EClient
from ibapi.contract import *
from ibapi.common import * # @UnusedWildImport
from ibapi.ticktype import * # @UnusedWildImport
from sources.marketdata.common.Configuration.Configuration import  Configuration
from sources.marketdata.common.Converters.SecurityConverter import SecurityConverter
from sources.marketdata.businessentities.MarketData import MarketData
from ibapi.connection import Connection
import configparser
import _thread
import time

class MarketDataClient(EWrapper):

    #region Private Consts
    INITIAL_MARKET_DATA_REQ_ID = 10000
    #endregion

    #region Constructors

    def __init__(self):
        self.id=None
        self.errorCode=None
        self.errorMsg=None
        self.MainManager = None
        self.OpenedSecurities = {}
        self.SecurityConverter = SecurityConverter()

    #endregion

    #region EWrapper Methods

    def tickPrice(self, reqId: TickerId, tickType: TickType, price: float,
                  attrib: TickAttrib):
        """Market data tick price callback. Handles all price related ticks."""
        if reqId in self.OpenedSecurities:
            sec = self.OpenedSecurities[reqId]
            self.SecurityConverter.AssignValue(sec,price,tickType)

    def tickSize(self, reqId: TickerId, tickType: TickType, size: int):
        """Market data tick size callback. Handles all size-related ticks."""
        pass

    def tickString(self, reqId: TickerId, tickType: TickType, value: str):
        pass

    def error(self,id, error, errorMsg):
        self.id=id
        self.errorCode=error
        self.errorMsg=errorMsg

    #endregion

    #region Private Methods

    def KeepRunning(self):
        self.ClientSocket.run()

    #endregion

    #region Public Methods

    def Initialize(self,pMainManager,pConfigFile):
        self.MainManager=pMainManager

        self.Configuration = Configuration()
        self.Configuration.LoadConfiguration(pConfigFile)

        # TODO Validar que se cargo bien la configuración
        self.ClientSocket = EClient(wrapper=self)

        self.ClientSocket.connect(self.Configuration.IP,self.Configuration.Port,1)

        _thread.start_new_thread(self.KeepRunning, ())

        _thread.start_new_thread(self.PublishMarketData, ())

    def PublishMarketData(self):
        while True:
            for sec in self.OpenedSecurities.values():
                self.MainManager.OnIncoming(sec.MarketData)
            time.sleep(1)


    def RequestMarketData (self,SecurityArr):

        reqId = MarketDataClient.INITIAL_MARKET_DATA_REQ_ID + 1
        for sec in SecurityArr:

            c = Contract()
            c.symbol = sec.Symbol
            c.secType = "STK"
            c.exchange = sec.Exchange
            c.currency = "USD"
            c.primaryExchange="ISLAND"

            self.OpenedSecurities.update({reqId: sec})
            self.ClientSocket.reqMktData(reqId, c, "", False, False, [])
            reqId +=1


    #endregion


