using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using zHFT.Main.Common.Generic;

public class LightSavingPeriodManager
{
    private readonly string _connectionString;

    public LightSavingPeriodManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<LightSavingPeriod> GetAll()
    {
        var result = new List<LightSavingPeriod>();

        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand("GetLigthSavingPeriods", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var period = BuildLightSavingPeriod(reader);
                    result.Add(period);
                }
            }
        }

        return result;
    }

    private LightSavingPeriod BuildLightSavingPeriod(SqlDataReader reader)
    {
        return new LightSavingPeriod
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
            OffsetHours = reader.GetInt32(reader.GetOrdinal("offset_hours"))
        };
    }
}
