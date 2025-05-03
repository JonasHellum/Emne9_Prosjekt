namespace Emne9_Prosjekt.GameComponents;

public class BattleShipComponents
{
    // Outdated variables. Replaced in SignalR. Left in code for testing.
    private int HitCount { get; set; }
    public int SunkenShipCount { get; set; }
    
    
    
    private string? _selectedShip;
    private bool _shipOrientation = true;
    private readonly Dictionary<string,int> _board = new ();
    private readonly Dictionary<string,int> _opponentBoard = new ();
    private readonly Dictionary<string, List<string>> _placedShips = new ();
    private readonly Dictionary<string,int> _ships = new ()
    {
        {"Carrier", 5},
        {"Battleship", 4},
        {"Cruiser", 3},
        {"Submarine", 3},
        {"Destroyer", 2}
    };

    public void CreateBoard()
    {
        char[] rows = "ABCDEFGHIJ".ToCharArray();
        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = 1; j <= 10; j++)
            {
                _board[$"{rows[i]}{j}"] = 0;
                _opponentBoard[$"{rows[i]}{j}"] = 0;
            }
        }
        Console.WriteLine("Begge brett er initialisert");
    }

    public void SelectShip(string ship)
    {
        if (_ships.ContainsKey(ship))
        {
            _selectedShip = ship;
        }
    }

    private List<string>? CheckAndStorePlacement(string position)
    {
        if (_selectedShip == null || !_ships.ContainsKey(_selectedShip)) return null;
        
        int size = _ships[_selectedShip];
        char row = position[0];
        int column = int.Parse(position[1..]);
        List<string> placement = new();
        
        for (int i = 0; i < size; i++)
        {
            string newPosition = _shipOrientation
                ? $"{row}{column + i}"
                : $"{(char)(row + i)}{column}";

            if (_board.ContainsKey(newPosition) && _board[newPosition] == 1) return null;
            
            placement.Add(newPosition);
        }
        return placement;
    }

    private List<string>? CheckAndAdjustPlacement(List<string>? positions)
    {
        if (positions == null || positions.Count == 0) return null;
        
        string position = positions[0];
        char row = position[0];
        int column = int.Parse(position[1..]);
        int size = positions.Count;

        if (_shipOrientation)
        {
            if (column + size - 1 > 10) column = 11 - size;
        }
        else
        {
            if (row + size - 1 > 'J') row = (char)('K' - size);
        }
        return CheckAndStorePlacement($"{row}{column}");
    }

    public void PlaceShip(string position)
    {
        if (_selectedShip == null || _placedShips.ContainsKey(_selectedShip)) return;
        
        var positions = CheckAndStorePlacement(position);
        positions = CheckAndAdjustPlacement(positions);

        if (positions == null) return;

        foreach (string pos in positions)
        {
            _board[pos]++;
        }
        _placedShips[_selectedShip] = positions;
        
        _selectedShip = null;
    }
    
    // Outdated function. Replaced by SignalR component. Left in code for testing.
    private void CheckShipStatus(string target)
    {
        foreach (var (_, positions) in _placedShips)
        {
            if (positions.Contains(target))
            {
                if (positions.All(pos => _board[pos] < 0))
                {
                    SunkenShipCount++;
                }
                break;
            }
        }
    }
    
    public void ToggleOrientation()
    {
        _shipOrientation = !_shipOrientation;
    }
    
    public Dictionary<string, int> GetBoard()
    {
        return _board;
    }

    // Not in use Just for testing
    public Dictionary<string, int> GetOpponentBoard()
    {
        return _opponentBoard;
    }
    
    public Dictionary<string, int> GetShips()
    {
        return _ships;
    }

    public Dictionary<string, List<string>> GetPlacedShips()
    {
        return _placedShips;
    }
    
    public bool GetOrientation()
    {
        return _shipOrientation;
    }
    
    // Outdated function. Replaced by SignalR component. Left in code for testing.
    public void ShootBoard(string target)
    {
        Console.WriteLine($"ShootBoard: Skyter på posisjon {target}");

        if (!_board.ContainsKey(target))
        {
            Console.WriteLine($"ShootBoard: Posisjon {target} finnes ikke på brettet");
            return;
        }

        int value = _board[target];
        Console.WriteLine($"ShootBoard: Verdi før skudd: {value}");

        if (_board[target] >= 0)
        {
            _board[target] -= 2;
            Console.WriteLine($"ShootBoard: Ny verdi etter skudd: {_board[target]}");
        }

        if (value == 1)
        {
            HitCount++;
            CheckShipStatus(target);
        }
    }

    public void UpdateOpponentBoard(string target, bool isHit)
    {
        Console.WriteLine($"UpdateOpponentBoard: Oppdaterer posisjon {target}, treff: {isHit}");

        if (_opponentBoard.ContainsKey(target))
        {
            // Oppdater motstanderens brett med resultatet av skuddet
            // -1 = treff, -2 = bom
            _opponentBoard[target] = isHit ? -1 : -2;
            Console.WriteLine($"UpdateOpponentBoard: Oppdatert eksisterende posisjon til {_opponentBoard[target]}");
        }
        else
        {
            // Hvis posisjonen ikke finnes i brettet, legg den til
            _opponentBoard.Add(target, isHit ? -1 : -2);
            Console.WriteLine($"UpdateOpponentBoard: Lagt til ny posisjon med verdi {_opponentBoard[target]}");
        }
    }
}