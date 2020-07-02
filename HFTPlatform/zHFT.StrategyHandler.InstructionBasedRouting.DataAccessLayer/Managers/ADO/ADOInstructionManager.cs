using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers.ADO
{
    public class ADOInstructionManager : IInstructionManagerAccessLayer
    {

        #region Protected Attributes

        protected string ConnectionString { get; set; }

        #endregion

        #region Constructores

        public ADOInstructionManager(string connectionString, IAccountManagerAccessLayer pAccountManager)
        {
            ConnectionString = connectionString;
        }

        #endregion


        #region Private Methods

        private Instruction BuildInstruction(SqlDataReader reader)
        {

            Instruction instr = new Instruction();

            instr.Id = Convert.ToInt32(reader["id"]);
            instr.Date = Convert.ToDateTime(reader["date"]);
            instr.InstructionType = new InstructionType() { Type = reader["type"].ToString(), Description = reader["type"].ToString() };


            instr.AccountPosition = reader["account_position_id"] != DBNull.Value ? new AccountPosition()
            {
                Id = Convert.ToInt32(reader["account_position_id"])
            } : null;

            instr.Account = new Account()
            {
                Id = Convert.ToInt32(reader["account_id"]),
                Customer = new Customer() { Id = Convert.ToInt32(reader["customer_id"]) },
                AccountNumber = Convert.ToInt64(reader["account_number"]),
                Broker = new Broker() { Id = Convert.ToInt32(reader["broker_id"]) },
                Name = reader["name"].ToString(),
                GenericAccountNumber = reader["generic_s_number"] != DBNull.Value ? reader["generic_s_number"].ToString() : null,
                Balance = reader["balance"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["balance"]) : null,
            };

            instr.Executed = Convert.ToBoolean(reader["executed"]);
            instr.Shares = reader["shares"] != DBNull.Value ? (int?)Convert.ToInt32(reader["shares"]) : null;
            instr.Ammount = reader["ammount"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["ammount"]) : null;
            instr.Symbol = reader["symbol"] != DBNull.Value ? Convert.ToString(reader["symbol"]) : null;
            //instr.QuoteSymbol = reader["quote_symbol"] != DBNull.Value ? Convert.ToString(reader["quote_symbol"]) : null;
            instr.SecurityType = Security.GetSecurityType(reader["sec_type"] != DBNull.Value ? reader["sec_type"].ToString() : null);
            instr.IsMerge = Convert.ToBoolean(reader["is_merge"]);
            instr.Text = Convert.ToString(reader["text"]);
            //instr.Steps = reader["steps"] != DBNull.Value ? (int?)Convert.ToInt32(reader["steps"]) : null;
            instr.Side = reader["side"] != DBNull.Value ? (Side?)Convert.ToChar(reader["side"]) : null;

            if (reader["related_instruction_id"] != DBNull.Value)
                instr.RelatedInstruction = new Instruction() { Id = Convert.ToInt32(reader["related_instruction_id"]) };

            return instr;
        }

        private Instruction DoReadSingle(SqlConnection Conn, SqlCommand cmd)
        {
            SqlDataReader reader = null;
            Instruction instr = null;

            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    instr = BuildInstruction(reader);
                }
            }
            catch
            {
                throw;

            }
            finally
            {
                reader.Close();
                Conn.Close();
            }

            return instr;
        }

        private List<Instruction> DoReadCollection(SqlConnection Conn, SqlCommand cmd)
        {
            SqlDataReader reader = null;
            List<Instruction> instrList = new List<Instruction>();

            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    instrList.Add(BuildInstruction(reader));
                }
            }
            catch
            {
                throw;

            }
            finally
            {
                reader.Close();
                Conn.Close();
            }

            return instrList;
        }

        private void DoReadRelInstruction(Instruction instr)
        {
            if (instr.RelatedInstruction != null)
                instr.RelatedInstruction = GetById(instr.RelatedInstruction.Id);
        }

        #endregion

        #region Public Methods

        public Instruction GetById(long instrId)
        {
            SqlConnection Conn = new SqlConnection(ConnectionString);
            Conn.Open();

            SqlCommand cmd = new SqlCommand("GetInstructions", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter symbolParam = cmd.Parameters.Add("@Id", SqlDbType.BigInt);
            symbolParam.Direction = ParameterDirection.Input;
            symbolParam.Value = instrId;

            Instruction instr = DoReadSingle(Conn, cmd);
            DoReadRelInstruction(instr);
            return instr;

        }

        public List<Instruction> GetPendingInstructions(long accountNumber)
        {
            SqlConnection Conn = new SqlConnection(ConnectionString);
            Conn.Open();

            SqlCommand cmd = new SqlCommand("GetInstructions", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter accParam = cmd.Parameters.Add("@AccountNumber", SqlDbType.BigInt);
            accParam.Direction = ParameterDirection.Input;
            accParam.Value = accountNumber;

            SqlParameter execParam = cmd.Parameters.Add("@Executed", SqlDbType.Bit);
            execParam.Direction = ParameterDirection.Input;
            execParam.Value = false;

            List<Instruction> instrx = DoReadCollection(Conn, cmd);
            if (instrx != null)
                instrx.ForEach(x => DoReadRelInstruction(x));
            return instrx;
        }

        public List<Instruction> GetRelatedInstructions(long accountNumber, int idSyncInstr, InstructionType type)
        {
            SqlConnection Conn = new SqlConnection(ConnectionString);
            Conn.Open();

            SqlCommand cmd = new SqlCommand("GetInstructions", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter accParam = cmd.Parameters.Add("@AccountNumber", SqlDbType.BigInt);
            accParam.Direction = ParameterDirection.Input;
            accParam.Value = accountNumber;

            SqlParameter execParam = cmd.Parameters.Add("@Executed", SqlDbType.Bit);
            execParam.Direction = ParameterDirection.Input;
            execParam.Value = false;

            SqlParameter relInstrParam = cmd.Parameters.Add("@RelatedInstrId", SqlDbType.Int);
            relInstrParam.Direction = ParameterDirection.Input;
            relInstrParam.Value = idSyncInstr;

            SqlParameter typeParam = cmd.Parameters.Add("@Type", SqlDbType.VarChar, 10);
            typeParam.Direction = ParameterDirection.Input;
            typeParam.Value = type.Type;

            List<Instruction> instrx = DoReadCollection(Conn, cmd);
            instrx.ForEach(x => DoReadRelInstruction(x));
            return instrx;
        }

        public void Persist(Instruction instr)
        {
            SqlConnection Conn = new SqlConnection(ConnectionString);

            try
            {
                using (SqlCommand cmd = Conn.CreateCommand())
                {
                    Conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PersistInstruction";
                    cmd.Parameters.Add(new SqlParameter("@InstructionId", instr.Id));
                    cmd.Parameters.Add(new SqlParameter("@Date", instr.Date));
                    cmd.Parameters.Add(new SqlParameter("@Type", instr.InstructionType.Type));

                    if (instr.AccountPosition != null)
                    {
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionId", instr.AccountPosition.Id));
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionStatus", instr.AccountPosition.PositionStatus.Code.ToString()));
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionActive", instr.AccountPosition.Active));
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionMarketPrice", instr.AccountPosition.MarketPrice));
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionShares", instr.AccountPosition.Shares));
                        cmd.Parameters.Add(new SqlParameter("@AccountPositionAmmount", instr.AccountPosition.Ammount));
                    }

                    if (instr.Symbol != null)
                        cmd.Parameters.Add(new SqlParameter("@Symbol", instr.Symbol));

                    if (instr.SecurityType != null)
                        cmd.Parameters.Add(new SqlParameter("@SecType", instr.SecurityType.ToString()));

                    if (instr.Shares != null)
                        cmd.Parameters.Add(new SqlParameter("@Shares", instr.Shares));

                    if (instr.Ammount != null)
                        cmd.Parameters.Add(new SqlParameter("@Ammount", instr.Ammount));

                    if (instr.Side != null)
                        cmd.Parameters.Add(new SqlParameter("@Side", instr.Side.ToString()));

                    if (instr.RelatedInstruction != null)
                        cmd.Parameters.Add(new SqlParameter("@RelatedInstructionId", instr.RelatedInstruction.Id));

                    cmd.Parameters.Add(new SqlParameter("@AccountId", instr.Account.Id));
                    cmd.Parameters.Add(new SqlParameter("@IsMerge", instr.IsMerge));
                    cmd.Parameters.Add(new SqlParameter("@Executed", instr.Executed));
                    cmd.Parameters.Add(new SqlParameter("@Text", instr.Text));
                    cmd.ExecuteNonQuery();
                }
                Conn.Dispose();
            }
            catch (Exception ex)
            {
                throw;

            }
            finally
            {
                if (Conn != null)
                    Conn.Close();
            }
        }

        #endregion
    }
}
