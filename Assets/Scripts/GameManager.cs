using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Игроки")]
    public PlayerController[] players;

    [Header("Настройки")]
    public bool useTimer = true;

    private float currentTime;
    private bool roundActive = false;
    private List<int> eliminationOrder = new List<int>();
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        if (FindObjectOfType<MainMenu>() == null)
            StartRound();
    }

    void Update()
    {
        if (!roundActive) return;
        if (useTimer)
        {
            currentTime += Time.deltaTime;
            if (uiManager != null) uiManager.UpdateTimer(Mathf.FloorToInt(currentTime));
        }
    }

    public void StartRound()
    {
        currentTime = 0;
        roundActive = true;
        eliminationOrder.Clear();

        // Уничтожаем PowerUp с прошлого раунда
        foreach (var pu in FindObjectsOfType<PowerUp>())
            Destroy(pu.gameObject);

        // ФИX: Сбрасываем таймеры нестабильных платформ.
        // Без этого timers[playerID] хранит время из прошлого раунда.
        // При респавне Time.time - timers[id] сразу > triggerTime => мгновенный выброс
        foreach (var up in FindObjectsOfType<UnstablePlatform>())
            up.ResetTimers();

        foreach (var p in players)
            p.gameObject.SetActive(true);

        if (uiManager != null)
        {
            uiManager.HideWinScreen();
            uiManager.UpdateTimer(0);
        }
    }

    public void PlayerEliminated(int playerNumber)
    {
        if (!roundActive) return;

        eliminationOrder.Add(playerNumber);
        if (uiManager != null) uiManager.ShowElimination(playerNumber);

        int active = 0; int last = -1;
        foreach (var p in players)
        {
            if (!p.IsEliminated()) { active++; last = p.playerNumber; }
        }

        if (active <= 1) EndRound(last);
    }

    void EndRound(int winner)
    {
        roundActive = false;

        var ranking = new List<int>();
        ranking.AddRange(players.Where(p => !p.IsEliminated()).Select(p => p.playerNumber));
        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
            ranking.Add(eliminationOrder[i]);

        if (uiManager != null)
            uiManager.ShowWinScreen(winner, new int[players.Length], ranking);
    }

    public bool IsRoundActive() => roundActive;
}