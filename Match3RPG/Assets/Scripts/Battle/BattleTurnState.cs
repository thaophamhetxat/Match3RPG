namespace Match3RPG.Battle
{
    public enum BattleTurnState
    {
        BattleStart,
        PlayerTurn,
        PlayerResolving,
        PlayerInvalidMove,
        PlayerTimedOut,
        BossCounterAttack,
        BossThinking,
        BossTurn,
        BossResolving,
        BoardShuffle,
        Victory,
        Defeat
    }

    public enum BattleActor
    {
        Player,
        Boss
    }
}