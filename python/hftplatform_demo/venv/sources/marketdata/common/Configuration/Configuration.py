import configparser

class Configuration:

    def __init__(self):
        self.Name = None
        self.AccountNumber = None
        self.IP = None
        self.Port = None
        self.Exchange = None


    def LoadConfiguration(self, configFile):
        config = configparser.ConfigParser()
        config.read(configFile)


        self.Name = config['DEFAULT']['Name']
        self.AccountNumber = int(config['DEFAULT']['AccountNumber'])
        self.IP =  config['DEFAULT']['IP']
        self.Port = int( config['DEFAULT']['Port'])
        self.Exchage = config['DEFAULT']['Exchange']


