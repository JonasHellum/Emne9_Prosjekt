using Emne9_Prosjekt.Hubs.Interfaces;

namespace UnitTestForBattleShip.PageTests.Components;

public class TestableGamePage
{
    
    public enum GameState
    {
        Setup,
        Playing,
        GameOver
    }
    public GameState _gameState = GameState.Setup;
    public Dictionary<string, int> _playerBoard = new();
    public Dictionary<string, bool> _opponentShipStatus = new();
    public IGameHubConnection GameHubConnection = default!;
    public Func<Dictionary<string, List<string>>> GetPlacedShips = default!;

    public string? _gameOverMessage;
    public bool _isWinner;

    public async Task CheckGameOver()
    {
        if (_gameState == GameState.GameOver)
            return;

        bool allMyShipsSunk = GetPlacedShips()
            .All(ship => ship.Value.All(pos => _playerBoard.TryGetValue(pos, out var v) && v < 0));

        if (allMyShipsSunk)
        {
            _gameState = GameState.GameOver;
            _isWinner = false;
            _gameOverMessage = "Du tapte! Alle dine skip er sunket.";
            await GameHubConnection.SendGameOverAsync(true);
            return;
        }

        if (_opponentShipStatus.Values.All(sunk => sunk) && _opponentShipStatus.Count > 0)
        {
            _gameState = GameState.GameOver;
            _isWinner = true;
            _gameOverMessage = "Du vant! Alle motstanderens skip er sunket.";
            await GameHubConnection.SendGameOverAsync(false);
        }
    }
}