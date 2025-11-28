public class RewardScoreEvent
{
    public int ScoreReward;
    public bool AllowMultiplier = true;
    public RewardScoreEvent(int scoreReward)
    {
        ScoreReward = scoreReward;
    }
}

public class NextWaveEvent
{
    public EnemyWave NextWave;
    public bool IsSilent = false;

    public NextWaveEvent(EnemyWave wave, bool silent)
    {
        NextWave = wave;
        IsSilent = silent;
    }
}

public class EnemyDropSpawnEvent
{
    public BasicEnemy Enemy;
    public EnemyDropSpawnEvent(BasicEnemy enemy)
    {
        this.Enemy = enemy;
    }
}

public class EnemyKilledEvent
{
    public BasicEnemy Enemy;
    public EnemyKilledEvent(BasicEnemy enemy)
    {
        this.Enemy = enemy;
    }
}