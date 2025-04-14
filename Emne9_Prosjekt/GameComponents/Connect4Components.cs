namespace Emne9_Prosjekt.GameComponents;

public class Connect4Components
{
    private readonly Dictionary<string,int> _board = new ();
    private string? _lastPiece;
    private bool _turnOrder = true;
    private bool _endGame;

    public void CreateBoard()
    {
        _endGame = false;
        char[] rows = "ABCDEF".ToCharArray();
        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = 1; j <= 7; j++)
            {
                _board[$"{rows[i]}{j}"] = 0;
            }
        }
    }
    
    public void DropPiece(string position)
    {
        int col = int.Parse(position[1..]);
        char[] rows = "FEDCBA".ToCharArray();

        foreach (char row in rows)
        {
            string pos = $"{row}{col}";
            if (_board[pos] == 0)
            {
                _board[pos] = _turnOrder ? 1 : 2;
                _lastPiece = pos;
                CheckRows();
                ToggleTurn();
                break;
            }
        }
    }

    private Dictionary<string, int> StoreRight()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            string pos = $"{row}{col + i}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }

    private Dictionary<string, int> StoreLeft()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            string pos = $"{row}{col - i}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }

    private Dictionary<string, int> CombineHorizontal()
    {
        Dictionary<string, int> storedValues = new ();
        
        Dictionary<string, int> left = StoreLeft();
        Dictionary<string, int> right = StoreRight();
        
        var combined = left.Keys
            .Concat(right.Keys)
            .Distinct()
            .OrderBy(x => int.Parse(x[1..]));

        foreach (var key in combined)
        {
            storedValues[key] = _board[key];
        }
        return storedValues;
    }

    private Dictionary<string, int> StoreUp()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row - i);
            string pos = $"{currentRow}{col}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> StoreDown()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row + i);
            string pos = $"{currentRow}{col}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> CombineVertical()
    {
        Dictionary<string, int> storedValues = new();
        
        Dictionary<string, int> up = StoreUp();
        Dictionary<string, int> down = StoreDown();
        
        var combined = up.Keys
            .Concat(down.Keys)
            .Distinct()
            .OrderBy(x => x[0]);

        foreach (var key in combined)
        {
            storedValues[key] = _board[key];
        }
        return storedValues;
    }
    
    private Dictionary<string, int> StoreTopLeft()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row - i);
            int currentCol = col - i;
            string pos = $"{currentRow}{currentCol}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> StoreBottomRight()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row + i);
            int currentCol = col + i;
            string pos = $"{currentRow}{currentCol}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> CombineDownRightDiagonal()
    {
        Dictionary<string, int> storedValues = new();
        
        Dictionary<string, int> topLeft = StoreTopLeft();
        Dictionary<string, int> bottomRight = StoreBottomRight();
        
        var combined = topLeft.Keys
            .Concat(bottomRight.Keys)
            .Distinct()
            .OrderBy(x => x[0])
            .ThenBy(x => int.Parse(x[1..]));

        foreach (var key in combined)
        {
            storedValues[key] = _board[key];
        }
        return storedValues;
    }
    
    private Dictionary<string, int> StoreBottomLeft()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row + i);
            int currentCol = col - i;
            string pos = $"{currentRow}{currentCol}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> StoreTopRight()
    {
        Dictionary<string, int> storedValues = new ();

        if (string.IsNullOrEmpty(_lastPiece)) return storedValues;
        
        char row = _lastPiece[0];
        int col = int.Parse(_lastPiece[1..]);

        for (int i = 0; i < 4; i++)
        {
            char currentRow = (char)(row - i);
            int currentCol = col + i;
            string pos = $"{currentRow}{currentCol}";
            if (_board.ContainsKey(pos))
            {
                storedValues[pos] = _board[pos];
            }
        }
        return storedValues;
    }
    
    private Dictionary<string, int> CombineUpRightDiagonal()
    {
        Dictionary<string, int> storedValues = new();
        
        Dictionary<string, int> bottomLeft = StoreBottomLeft();
        Dictionary<string, int> topRight = StoreTopRight();
        
        var combined = bottomLeft.Keys
            .Concat(topRight.Keys)
            .Distinct()
            .OrderBy(x => x[0])
            .ThenBy(x => int.Parse(x[1..]));

        foreach (var key in combined)
        {
            storedValues[key] = _board[key];
        }
        return storedValues;
    }
    
    private void CheckRows()
    {
        if (string.IsNullOrEmpty(_lastPiece)) return;
        
        int targetCheck = _board[_lastPiece];
        if (targetCheck == 0) return;
        
        var horizontalChain = CombineHorizontal();
        var verticalChain = CombineVertical();
        var downRightDiagonal = CombineDownRightDiagonal();
        var upRightDiagonal = CombineUpRightDiagonal();

        foreach (var chain in new []
                 {
                     horizontalChain,
                     verticalChain,
                     downRightDiagonal,
                     upRightDiagonal
                 })
        {
            int count = 0;
            foreach (var value in chain.Values)
            {
                if (value == targetCheck)
                {
                    count++;
                    if (count >= 4)
                    {
                        _endGame = true;
                        return;
                    }
                }
                else
                {
                    count = 0;
                }
            }
        }
    }
    
    private void ToggleTurn()
    {
        _turnOrder = !_turnOrder;
    }
    
    public Dictionary<string, int> GetBoard()
    {
        return _board;
    }

    public bool GameEnd()
    {
        return _endGame;
    }
}