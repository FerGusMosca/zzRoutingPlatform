using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.DataAccess;


namespace zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers
{
    public class InstructionManager : MappingEnabledAbstract
    {
        #region Constructors

        public InstructionManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(instructions instrxDB, Instruction instr)
        {
            instr.Id = instrxDB.id;
            instr.Date = instrxDB.date;
            instr.InstructionType = new InstructionType() { Type = instrxDB.instruction_types.type, Description = instrxDB.instruction_types.description };
            //Falta asignar el atributo del Model Portfolio

            if (instrxDB.account_position_id != null)
                instr.AccountPosition = new AccountPosition()
                {
                    Id = instrxDB.account_positions.id
                };

            instr.Account = new Account()
            {
                Id = instrxDB.account_id,
                AccountNumber = instrxDB.accounts.account_number,
                IBAccount = instrxDB.accounts.ib_account,
                IBBalance = instrxDB.accounts.ib_balance,
                IBCurrency=instrxDB.accounts.ib_currency,
                IBPort = instrxDB.accounts.ib_port,
                IBURL = instrxDB.accounts.ib_url,
            };

            instr.Executed = instrxDB.executed;
            instr.Shares = instrxDB.shares;
            instr.Ammount = instrxDB.ammount;
            instr.Symbol = instrxDB.symbol;
            instr.SecurityType = Security.GetSecurityType(instrxDB.sec_type);

            instr.IsMerge = instrxDB.is_merge;
            instr.Text = instrxDB.text;

            if (instrxDB.side != null)
                instr.Side = (Side)Convert.ToChar(instrxDB.side);

            if (instrxDB.related_instruction_id != null)
                instr.RelatedInstruction = Map(instrxDB.instructions2);
        }

        private void FieldMap(Instruction instr, instructions instrDB)
        {
            instrDB.id = instr.Id;
            instrDB.date = instr.Date;
            instrDB.type = instr.InstructionType.Type;
            //Falta asignar el atributo del model portfolio

            instrDB.is_merge = instr.IsMerge;
            instrDB.text = instr.Text;

            instrDB.account_id = instr.Account.Id;
            instrDB.executed = instr.Executed;

            if (instr.Side != null)
                instrDB.side = instr.Side.ToString();

            if (instr.RelatedInstruction != null)
                instrDB.related_instruction_id = instr.RelatedInstruction.Id;

            
        }

        private void FieldMap(AccountPosition pos, account_positions posDB)
        {
            posDB.id = pos.Id;
            if(pos.Account!=null)
                posDB.account_id = pos.Account.Id;

            if(pos.Stock!=null)
                posDB.symbol = pos.Stock.Symbol;

            posDB.weight = pos.Weight;
            posDB.shares = pos.Shares;
            posDB.market_price = pos.MarketPrice;
            posDB.ammount = pos.Ammount;

            if(pos.PositionStatus!=null)
                posDB.status = pos.PositionStatus.Code.ToString();

            posDB.active = pos.Active;
        }

        private Instruction Map(instructions instrDB)
        {
            Instruction instr = new Instruction();
            FieldMap(instrDB, instr);
            return instr;
        }

        private instructions Map(Instruction instr)
        {
            instructions instrDB = new instructions();
            FieldMap(instr, instrDB);
            return instrDB;
        }

        #endregion

        #region Public Methods

        public Instruction GetById(long instrId)
        {
            instructions instrxDB = ctx.instructions.Where(x => x.id == instrId).FirstOrDefault();

            if (instrxDB != null)
            {
                Instruction instr = new Instruction();
                FieldMap(instrxDB, instr);
                return instr;
            }
            else
                return null;

        }

        public List<Instruction> GetPendingInstructions(long accountNumber)
        {
            List<Instruction> instructions = new List<Instruction>();
            List<instructions> instructionsDB = ctx.instructions.Where(x => x.accounts.account_number == accountNumber && !x.executed).ToList();
            foreach (instructions instrxDB in instructionsDB)
            {
                Instruction instr = new Instruction();
                FieldMap(instrxDB, instr);
                instructions.Add(instr);
            }
            return instructions;
        }

        public List<Instruction> GetRelatedInstructions(long accountNumber,int idSyncInstr,InstructionType type)
        {
            List<Instruction> instructions = new List<Instruction>();
            List<instructions> instructionsDB = ctx.instructions.Where(x =>    x.accounts.account_number == accountNumber 
                                                                            && !x.executed
                                                                            && x.related_instruction_id== idSyncInstr
                                                                            && x.type==type.Type).ToList();

            foreach (instructions instrxDB in instructionsDB)
            {
                Instruction instr = new Instruction();
                FieldMap(instrxDB, instr);
                instructions.Add(instr);
            }
            return instructions;
        
        }

        public void Persist(Instruction instr)
        {
            //Insert
            if (instr.Id == 0)
            {
                instructions instrDB = Map(instr);
                ctx.instructions.AddObject(instrDB);

                if (instr.AccountPosition != null)
                {
                    account_positions posDB = ctx.account_positions.Where(x => x.id == instr.AccountPosition.Id).FirstOrDefault();
                    FieldMap(instr.AccountPosition, posDB);
                }

                ctx.SaveChanges();
                instr.Id = instrDB.id;
            }
            else
            {
                instructions instrDB = ctx.instructions.ToList().Where(x => x.id == instr.Id).FirstOrDefault();
                FieldMap(instr, instrDB);

                if (instr.AccountPosition != null && instr.AccountPosition.Account!=null)
                {
                    account_positions posDB = ctx.account_positions.Where(x => x.id == instr.AccountPosition.Id).FirstOrDefault();
                    FieldMap(instr.AccountPosition, posDB);
                }

                ctx.SaveChanges();
            }
        }

      
        #endregion
    }
}
