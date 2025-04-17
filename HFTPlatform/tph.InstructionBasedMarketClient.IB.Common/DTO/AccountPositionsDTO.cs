using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Positions;

public class AccountPositionsDTO
{
    public List<Position> SecurityPositions { get; private set; } = new List<Position>();
    public List<Position> LiquidPositions { get; private set; } = new List<Position>();

    public bool ReceivedSecurityPositions { get; set; } = false;
    public bool ReceivedLiquidPositions { get; set; } = false;

    private readonly object _lock = new object();

    public void AddSecurityPosition(Position pos)
    {
        lock (_lock)
        {
            SecurityPositions.Add(pos);
        }
    }

    public void AddLiquidPosition(Position pos)
    {
        lock (_lock)
        {
            LiquidPositions.Add(pos);
        }
    }

    public bool IsComplete()
    {
        return ReceivedSecurityPositions && ReceivedLiquidPositions;
    }

    public List<Position> GetSecurityPositions()
    {
        lock (_lock)
        {
            return new List<Position>(SecurityPositions);
        }
    }

    public List<Position> GetLiquidPositions()
    {
        lock (_lock)
        {
            return new List<Position>(LiquidPositions);
        }
    }
}
