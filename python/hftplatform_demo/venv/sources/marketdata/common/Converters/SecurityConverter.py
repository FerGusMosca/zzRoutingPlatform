import sys
sys.path.insert(0, ".\\venv\\IBApis")
from ibapi.ticktype import * # @UnusedWildImport

class SecurityConverter:
    def __init__(self):
        pass


    def AssignValue(self,Security,price: float, tickType: TickType):
        if tickType == TickTypeEnum.CLOSE:
           Security.MarketData.Close = price
        elif tickType==TickTypeEnum.HIGH:
            Security.MarketData.High= price
        elif tickType == TickTypeEnum.LOW:
            Security.MarketData.Low = price
        elif tickType == TickTypeEnum.OPEN:
            Security.MarketData.Open = price