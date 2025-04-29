namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IConnectFourGameHubClientMethods
{
    Task WaitingForOpponent();
    Task StartGame(Dictionary<string, int>board,bool isMyTurn);
    Task GameOver(bool youWon);
    Task OpponentDisconnected();
    Task UpdateBoard(string position);
}