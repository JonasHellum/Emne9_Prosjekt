namespace Emne9_Prosjekt.Game_components;

public class BattleShipComponents
{
    private string? _selectedShip;
    private bool _shipOrientation = true;
    public int HitCount { get; private set; }
    private readonly Dictionary<string,int> _board = new ();
    private readonly Dictionary<string,int> _opponentBoard = new ();
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
            }
        }
    }

    public void SelectShip(string ship)
    {
        if (_ships.ContainsKey(ship))
        {
            _selectedShip = ship;
        }
    }

    public void PlaceShip(string position)
    {
        if (_selectedShip == null || !_ships.ContainsKey(_selectedShip))
        {
            return;
        }
        int size = _ships[_selectedShip];
        char row = position[0];
        int column = int.Parse(position[1].ToString());
        
        List<string> newPositions = new ();
        for (int i = 0; i < size; i++)
        {
            string newPosition;
            if (_shipOrientation)
            {
                newPosition = $"{row}{column + i}";
            }
            else
            {
                char newRow = (char)(row + i);
                newPosition = $"{newRow}{column}";
            }
            if (_board.ContainsKey(newPosition) && _board[newPosition] == 1)
            {
                return;
            }
            newPositions.Add(newPosition);
        }
        foreach (string pos in newPositions)
        {
            _board[pos]++;
        }
        //_selectedShip = null;
    }
    
    public void ToggleOrientation()
    {
        _shipOrientation = !_shipOrientation;
    }
    
    public Dictionary<string, int> GetBoard()
    {
        return _board;
    }
    
    public Dictionary<string, int> GetShips()
    {
        return _ships;
    }

    public Dictionary<string, int> GetOpponentBoard()
    {
        return _opponentBoard;
    }
    
    public bool GetOrientation()
    {
        return _shipOrientation;
    }
    
    public void ShootBoard(string position)
    {
        int value = _board[position];
        
        if (_board.ContainsKey(position))
        {
            if (_board[position] >= 0)
            {
                _board[position] -= 2;
            }

            if (value == 1)
            {
                HitCount++;
            }
        }
    }
}