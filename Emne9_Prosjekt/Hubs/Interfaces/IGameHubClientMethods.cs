namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IGameHubClientMethods
{
    Task WaitingForOpponent();
    Task StartGame(Dictionary<string, int> opponentBoard, bool isMyTurn);
    Task UpdateShot(string position, string shooterId, bool isHit);
    Task UpdateShipStatus(string shipName, bool isSunk);
    Task GameOver(bool youWon);
    Task OpponentDisconnected();
    Task UpdateOpponentBoardHit(string position); //Denne brukes ikke foreløpig
}