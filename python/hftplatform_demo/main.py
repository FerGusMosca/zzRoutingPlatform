import json
import msvcrt
import configparser
import importlib
from sources.marketdata.businessentities.Security import Security
from sources.marketdata.MarketDataClient import MarketDataClient


class MainManager():

    def __init__(self):
        self.MarketDataClient =None

    def Initialize (self,configFile):

        config = configparser.ConfigParser()
        config.read(configFile)

        module_name, class_name = config['DEFAULT']['INCOMING_MODULE'].rsplit(".", 1)
        MarketDataClientClass = getattr(importlib.import_module(module_name), class_name)
        incomingMDConfigFile = config['DEFAULT']['INCOMING_CONFIG_FILE']

        self.MarketDataClient = MarketDataClientClass()
        self.MarketDataClient.Initialize(self,incomingMDConfigFile)

        if self.MarketDataClient.ClientSocket.wrapper.errorCode is None:
            print("Successful connection")
        else:
            print(
                "{} - Error {}".format(MarketDataClient.ClientSocket.wrapper.errorCode,
                                       MarketDataClient.ClientSocket.wrapper.errorMsg))

    def OnIncoming(self,MarketData):
        print("Se recibió market data: Symbol {} Open {} High {} Low {} Close {}"
              .format(MarketData.Security.Symbol,MarketData.Open,MarketData.High,MarketData.Low,MarketData.Close))

    def RequestMarketData(self, securities):
        self.MarketDataClient.RequestMarketData(securities)


if __name__ == '__main__':

    mainManager = MainManager()
    mainManager.Initialize("venv\\configs\\main.ini")

    #We request market data in a file
    secs=[]
    with open('venv\\configs\\input.json') as f:
        loaded_json = json.load(f)
        for security in loaded_json:
            secs.append(Security(**security))

    mainManager.RequestMarketData(secs)

    msvcrt.getch()

