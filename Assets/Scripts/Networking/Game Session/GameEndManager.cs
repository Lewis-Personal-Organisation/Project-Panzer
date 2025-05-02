using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameEndManager : Singleton<GameEndManager>
{
    [SerializeField] GameplaySceneManager gameSceneManager;

    void Awake()
    {
        base.Awake();
    }

    public void HostGameOver()
    {
        var gameResultsData = CompileScores();

        gameResultsData = DetermineWinner(gameResultsData);

        var gameResultsJson = JsonUtility.ToJson(gameResultsData);

        gameSceneManager.ShowGameTimer(0);

        // By using the results passed from host, we ensure all players show the same results and allow
        // the host to pick a random winner if players tie.
        var results = JsonUtility.FromJson<DataStructs.GameResultsData>(gameResultsJson);
        GameplayNetworkManager.Instance?.OnGameOver(gameResultsJson);
    }

    DataStructs.GameResultsData CompileScores()
    {
        var playerScores = new List<DataStructs.PlayerScoreData>();
        var gameResultsData = new DataStructs.GameResultsData()
        {
            playerScoreData = playerScores,
        };

        var playerAvatars = GameplayNetworkManager.Instance.playerAvatars;
        foreach (var playerAvatar in playerAvatars)
        {
            playerScores.Add(new DataStructs.PlayerScoreData(playerAvatar));
        }

        return gameResultsData;
    }

    DataStructs.GameResultsData DetermineWinner(DataStructs.GameResultsData gameResultsData)
    {
        gameResultsData.winnerScore = int.MinValue;

        // We count ties so we can randomly select a winner based on number of tieing players. For example, if
        // 3 players tie, each has a 33% chance so, when we encounter the 3rd tie, we give them a 1 in 3 chance.
        var numTies = 1;

        var playerAvatars = GameplayNetworkManager.Instance.playerAvatars;
        foreach (var playerAvatar in playerAvatars)
        {
            if (playerAvatar.score > gameResultsData.winnerScore)
            {
                gameResultsData.winnerPlayerName = playerAvatar.playerName;
                gameResultsData.winnerPlayerId = playerAvatar.playerId;
                gameResultsData.winnerScore = playerAvatar.score;
                numTies = 1;
            }
            else if (playerAvatar.score == gameResultsData.winnerScore)
            {
                // Base chance of each new tieing player winning on count of players that have tied so, if this
                // is the 2nd tie, they're given a 1 in 2 chance of winning, and the 3rd receives a 1 in 3.
                numTies++;
                if (UnityEngine.Random.Range(0, numTies) == 0)
                {
                    gameResultsData.winnerPlayerName = playerAvatar.playerName;
                    gameResultsData.winnerPlayerId = playerAvatar.playerId;
                    gameResultsData.winnerScore = playerAvatar.score;
                }
            }
        }

        return gameResultsData;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("ServerlessMultiplayerGameSample");
    }
}