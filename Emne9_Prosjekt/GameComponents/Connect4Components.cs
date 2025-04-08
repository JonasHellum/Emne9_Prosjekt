namespace Emne9_Prosjekt.GameComponents;

public class Connect4Components
{
    private readonly Dictionary<string,int> _board = new ();
    private bool _turnOrder = true;
    
    public void CreateBoard()
    {
        char[] rows = "ABCDEF".ToCharArray();
        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = 1; j <= 7; j++)
            {
                _board[$"{rows[i]}{j}"] = 0;
            }
        }
    }

    private void ToggleTurn()
    {
        _turnOrder = !_turnOrder;
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
                ToggleTurn();
                break;
            }
        }
    }
    
    public Dictionary<string, int> GetBoard()
    {
        return _board;
    }
}