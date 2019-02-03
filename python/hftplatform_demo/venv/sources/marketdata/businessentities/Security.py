from sources.marketdata.businessentities.MarketData import MarketData

class Security:
    def __init__(self, Symbol, Exchange):
        self.Symbol=Symbol
        self.Exchange= Exchange
        self.MarketData= MarketData(self)