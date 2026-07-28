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

/// <summary>
/// The run's wave counter changed (S203). Separate from <see cref="NextWaveEvent"/> on purpose:
/// that one carries the wave ASSET and drives real gameplay (perk rolls, difficulty scaling, stat
/// upgrades, powerup spawns), so it must not be dispatched just to refresh a label -- and it is
/// not dispatched at all when a miniboss takes the wave's place, which is precisely a moment the
/// HUD needs to update.
/// </summary>
public class RunWaveChangedEvent
{
    /// <summary>Waves survived so far. The HUD shows the wave being fought, so it adds one.</summary>
    public int WavesCleared;

    public RunWaveChangedEvent(int wavesCleared)
    {
        WavesCleared = wavesCleared;
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