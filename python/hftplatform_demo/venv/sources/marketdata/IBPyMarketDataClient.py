import sys
sys.path.insert(0, ".\\venv\\IBApis")
from ib.ext.Contract import Contract
from ib.ext.Order import Order
from ib.opt import Connection, message

class IBPyMarketDataClient():

    def __init__(self, configFile):
        self.id=None
        self.errorCode=None
        self.errorMsg=None
        self.MainManager = None

        self.Configuration = Configuration()
        self.Configuration.LoadConfiguration(configFile)

        #TODO Validar que se cargo bien la configuración
        self.ClientSocket = EClient(wrapper=self)

    def error_handler(msg):
        """Handles the capturing of error messages"""
        print
        "Server Error: %s" % msg

    def reply_handler(msg):
        """Handles of server replies"""
        print
        "Server Response: %s, %s" % (msg.typeName, msg)


    def Initialize(self,pMainManager):
        self.MainManager=pMainManager

        self.tws_conn = Connection.create(port=7496, clientId=100)
        self.tws_conn.connect()



    def RequestMarketData (self,MarketDataReqArr):

        for mdReq in MarketDataReqArr:
            c = Contract()
            c.symbol = "xxx"
            c.secType = "STK"
            c.exchange = mdReq.Exchange
            c.currency = "USD"
            c.primaryExchange="ISLAND"



