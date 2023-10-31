using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccess;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers
{
    public class PositionManager : MappingEnabledAbstract
    {
        #region Constructors
        public PositionManager(AutPortfolioEntities context) : base(context) { }

        public PositionManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(AccountPosition pos, account_positions posDB)
        {
            posDB.account_id = pos.Account.Id;
            posDB.active = pos.Active;
            posDB.ammount = pos.Ammount;
            posDB.market_price = pos.MarketPrice;
            posDB.status = pos.PositionStatus.Code.ToString();
            posDB.shares = pos.Shares;
            posDB.symbol = pos.Security.Symbol;
            posDB.weight = pos.Weight;
        }

        private void FieldMap(account_positions posDB, AccountPosition pos)
        {
            pos.Id = posDB.id;
            pos.Account = new Account() { Id = posDB.account_id };
            pos.Active = posDB.active;
            pos.Ammount = posDB.ammount;
            pos.MarketPrice = posDB.market_price;
            pos.PositionStatus = new PositionStatus() { Code = Convert.ToChar(posDB.status) };
            pos.Shares = posDB.shares;
            pos.Security = new Security() { Symbol = posDB.symbol };
            pos.Weight = posDB.weight;
        }

        private account_positions Map(AccountPosition pos)
        {
            account_positions posDB = new account_positions();
            FieldMap(pos, posDB);
            return posDB;
        }


        #endregion

        #region Public Methods

        public AccountPosition GetActivePositionBySymbol(string symbol)
        {

            account_positions posDB = ctx.account_positions.Where(x => x.symbol == symbol
                                                                        && x.active == true).FirstOrDefault();
            if (posDB != null)
            {
                AccountPosition pos = new AccountPosition();
                FieldMap(posDB, pos);
                return pos;
            }
            else
                return null;

        }

        public AccountPosition GetById(long id)
        {

            account_positions posDB = ctx.account_positions.Where(x => x.id == id).FirstOrDefault();
            if (posDB != null)
            {
                AccountPosition pos = new AccountPosition();
                FieldMap(posDB, pos);
                return pos;
            }
            else
                return null;

        }

        protected void DeleteAllOnline()
        {
            List<account_positions> positionsDB = ctx.account_positions.Where(x => x.status == PositionStatus._S_EXECUTED).ToList();
            positionsDB.ForEach(x => ctx.account_positions.DeleteObject(x));
        }

        public void Update(AccountPosition pos)
        {
            account_positions posDB = ctx.account_positions.ToList().Where(x => x.id == pos.Id).FirstOrDefault();
            FieldMap(pos, posDB);
            ctx.SaveChanges();
        }

        public void Persist(AccountPosition pos)
        {
            //Insert
            if (pos.Id == 0)
            {
                account_positions posDB = Map(pos);
                ctx.account_positions.AddObject(posDB);
                ctx.SaveChanges();
                pos.Id = posDB.id;
            }
            else
                Update(pos);
        }

        public void PersistAndReplace(List<AccountPosition> positions)
        {
            DeleteAllOnline();

            foreach (AccountPosition pos in positions)
            {
                //Insert
                if (pos.Id == 0)
                {
                    account_positions posDB = Map(pos);
                    ctx.account_positions.AddObject(posDB);
                    pos.Id = posDB.id;
                }
                else
                {
                    account_positions posDB = ctx.account_positions.ToList().Where(x => x.id == pos.Id).FirstOrDefault();
                    FieldMap(pos, posDB);
                }

            }
            ctx.SaveChanges();

        }


        #endregion
    }
}
