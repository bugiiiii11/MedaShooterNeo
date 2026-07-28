using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : TimeCompute
{
    public EnemyWavesProfile Profile;
    public EnemyWavesProfile UnendingProfile;
    public EnemyWavesProfile Level2Profile;

    public Transform BossSpawnPoint, MinesHolder;
    public List<Transform> SpawnPositions => LevelInfo.instance.SpawnPositions;
    public Transform MostLeftEnemyKiller;

    public int currentActiveWave = 0;

    public float SpawnCooldown = 0.5f;
    private float minesSpawnCooldown = 0, minesCurrentSpawnCooldown = 0;

    private int currentEnemyCount, spawnedEnemiesCount;

    private float lastSpawnTime = 0;

    public bool IsDefaultWave = true;

    private bool isBossSpawned = false, isMinibossSpawned = false;

    public int increaseNumberOfEnemies = 0, waveNumber = 0;
    public float SpawnRateFactor = 1;

    public float ShootingSpeedFactor = 1;

    [System.NonSerialized]
    internal Transform AllEnemies;

#if UNITY_EDITOR
    [Header("Head start")]
    public int StartAtWave = 0;
#endif

    private void Start()
    {
        ComputeTime();
        AllEnemies = new GameObject("Enemies").transform;
        currentEnemyCount = 0;
        spawnedEnemiesCount = 0;

        // Check for Level 2 and swap profile if needed
        if (PlayerPrefs.GetInt("IsLevel2", 0) == 1)
        {
            if (Level2Profile != null)
            {
                Profile = Level2Profile;
                Debug.Log("🎮 Level 2 activated - using Level2Profile");
            }

            // Clear the flag after reading
            PlayerPrefs.SetInt("IsLevel2", 0);
            PlayerPrefs.Save();
        }

        InvokeRepeating(nameof(CheckForMissingEnemies), 5, 5);

#if UNITY_EDITOR
        if (StartAtWave > 0)
            Headstart(StartAtWave);

        //// compute max score that can be achieved
        //var max = ComputeMaxScore();
        //// add endless wave that is being played for hour
        //var maxScoreInEndless = 50 * 5;
        //var enemiesPerSecond = 0.4f;
        //var playtimeInSeconds = 1 * 3600;
        //var allEnemies = enemiesPerSecond * playtimeInSeconds;
        //var endlessScore = Mathf.FloorToInt(allEnemies) * maxScoreInEndless;

        //print(max + endlessScore);
#endif
    }

    /// <summary>
    /// Clears one enemy off the field, keeping <see cref="currentEnemyCount"/> honest.
    /// Returns whether anything was actually cleared.
    /// </summary>
    /// <remarks>
    /// The field sweeps used to call SetActive(false) directly. That never fires
    /// BasicEnemy.OnSendKilledDataToSpawner, so every enemy a boss or miniboss wiped stayed
    /// counted as alive forever -- and both bosses sweep the field on spawn. Once the phantom
    /// count reached MaxEnemyCount the spawn gate in Update() stopped opening and the rest of
    /// the run played out empty, with CheckForMissingEnemies advancing a wave every 15s. That
    /// was the empty-wave bug (found S201).
    /// Deliberately NOT routed through BasicEnemy.Kill(): these sweeps are silent by design and
    /// Kill() awards score and rolls drops.
    /// </remarks>
    private bool ClearEnemy(Transform enemy)
    {
        if (!enemy.gameObject.activeSelf)
            return false;

        // An enemy already playing its death animation has fired the event and been
        // subtracted; subtracting twice would under-count and over-spawn.
        var basicEnemy = enemy.GetComponent<BasicEnemy>();
        var alreadySubtracted = basicEnemy != null && basicEnemy.IsDead;

        GameEffectsPool.SpawnNormalExplosionMuted(enemy.position, 1);
        enemy.gameObject.SetActive(false);

        if (!alreadySubtracted)
            currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);

        return true;
    }

    internal void KillAllEnemies(GameObject except)
    {
        var killedAny = false;
        foreach (Transform enemy in AllEnemies)
        {
            if (enemy.gameObject == except.transform.parent.gameObject)
                continue;

            if (ClearEnemy(enemy))
                killedAny = true;
        }

        if(killedAny)
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.GameOverSound, 0.6f);
    }

    internal void KillAllEnemies()
    {
        foreach (Transform enemy in AllEnemies)
        {
            ClearEnemy(enemy);
        }
        OneShotAudioPool.SpawnOneShot(LevelProps.instance.GameOverSound, 0.6f);
    }
    public bool IsBossActive()
    {
        return isBossSpawned || isMinibossSpawned;
    }

    private void CheckForMissingEnemies()
    {
        if (IsBossActive())
        {
            return;
        }

        var durationSinceLastSpawn = Time.time - lastSpawnTime;
        var currentWave = Profile.Waves[currentActiveWave];

        if (durationSinceLastSpawn > 15 && !GameManager.instance.IsGamePaused)
        {
            // there is some error
            // Nothing has spawned for 15s, so the spawn gate is stuck. Resync the live count
            // against reality BEFORE advancing: this used to only mask a leaking counter by
            // rolling the wave over, which is why the bug read as "some waves are empty"
            // instead of "the run is over". Any future leak now heals within 15s (S201).
            ResyncEnemyCount();
            NextWave(currentWave);
            lastSpawnTime = Time.time;
        }
    }

    /// <summary>
    /// Recomputes <see cref="currentEnemyCount"/> from the enemies actually on the field.
    /// Only safe to call with no boss active -- a miniboss is parented under AllEnemies but
    /// never increments the counter, so it would be counted twice. Both callers of this are
    /// already behind an IsBossActive() guard.
    /// </summary>
    private void ResyncEnemyCount()
    {
        var alive = 0;
        foreach (Transform enemy in AllEnemies)
        {
            if (!enemy.gameObject.activeSelf)
                continue;

            var basicEnemy = enemy.GetComponent<BasicEnemy>();
            if (basicEnemy != null && basicEnemy.IsDead)
                continue;

            alive++;
        }

        currentEnemyCount = alive;
    }

    public void Headstart(int index)
    {
        for (var i = 0; i < index; i++)
        {
            currentActiveWave = GetIndexForNextWave();

            //if (!Profile.Waves[currentActiveWave].IsSilent)
            //{
            //    GameManager.instance.EventManager.Dispatch(new NextWaveEvent(Profile.Waves[currentActiveWave]));
            //}
            AdvanceWaveNumber();
            ScaleActiveEnemiesNumberCount(waveNumber);

            var isSilent = Profile.Waves[currentActiveWave].IsSilent;
            GameManager.instance.EventManager.Dispatch(new NextWaveEvent(Profile.Waves[currentActiveWave], isSilent));
        }

        if((index % 5) == 0 && waveNumber > 10)
        {
            SpawnMiniboss();
        }

        // simulate speed
        GameManager.instance.GameConstants.GameSpeedMultiplier = Mathf.Clamp(0.016f * (index * 45 - 1 * 200) / 2, 1.1f, 1.75f);

        // give random perks
        UIPerkManager.instance.GiveRandomPerks(index);
    }

    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        if (isMinibossSpawned)
        {
            return;
        }

        var currentWave = Profile.Waves[currentActiveWave];

        if(TimeSinceLastCompute > SpawnCooldown && currentEnemyCount < currentWave.MaxEnemyCount + increaseNumberOfEnemies)
        {
            // compute next cooldown
            CalculateSpawnTime(currentWave);
            ComputeTime();

            // spawn enemy
            var enemy = currentWave.GetEnemyRandomByProbability();

            // Belt and braces against unspawnable wave data. Without this, a wave whose enemies
            // have no prefab reaches Instantiate(null) and throws -- before currentEnemyCount++,
            // before spawnedEnemiesCount++ and before lastSpawnTime is refreshed, so the wave can
            // never advance itself and the throw repeats every frame until CheckForMissingEnemies
            // rolls it over 15-20s later. The endless profile shipped exactly such a wave.
            // Rerolling costs one frame instead.
            if (enemy == null || enemy.Prefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Wave {currentActiveWave} has no spawnable enemy; skipping to the next wave.");
                NextWave(currentWave);
                return;
            }

            GameObject obj;

            if (currentWave.IsBoss)
            {
                if (isBossSpawned)
                    return;

                // destroy mines
                foreach(Transform mine in MinesHolder)
                {
                    mine.GetComponent<Mine>().Explode();
                }

                obj = Instantiate(enemy.Prefab, BossSpawnPoint.position, Quaternion.identity);
                obj.transform.SetParent(AllEnemies);

                var bossEnemy = obj.GetComponentInChildren<BasicBoss>();
                bossEnemy.Initialize(this);
                bossEnemy.SetParams(enemy);

                isBossSpawned = true;
            }
            else
            {
                if (UINumbersHandler.instance.FullScore > 17000)
                {
                    var multi = Mathf.Clamp(UINumbersHandler.instance.FullScore / 17000f, 1, 1.5f);

                    if (Random.value < 0.3f * multi)
                    {
                        SpawnEnemyForHackers(enemy, multi);
                        currentEnemyCount++;
                        spawnedEnemiesCount++;
                        GameManager.instance.GameStats.EnemiesSpawned++;
                    }
                }
                SpawnEnemy(enemy);
            }

            currentEnemyCount++;
            spawnedEnemiesCount++;
            GameManager.instance.GameStats.EnemiesSpawned++;
        }

        // no enemy?
        if (currentEnemyCount == 0)
        {
            SpawnCooldown -= Time.deltaTime * 5;
        }

            // handle mines
        if (currentWave.SpawnMines)
        {
            if (Time.time - minesCurrentSpawnCooldown > minesSpawnCooldown)
            {
                minesSpawnCooldown = currentWave.MineSpawnCooldownRange.Random();
                minesCurrentSpawnCooldown = Time.time;

                // rand spawn pos between top and bot
                var topY = LevelInfo.instance.BoundTop.position.y;
                var botY = LevelInfo.instance.BoundBottom.position.y;
                var a = Mathf.Min(topY, botY);
                var b = Mathf.Max(topY, botY);

                var randY = Random.Range(a, b);
                var x = LevelInfo.instance.SpawnPositions[0].position.x;

                SpawnMine(new Vector3(x, randY, 0));
            }
        }

        // handle enemy switches
        if (spawnedEnemiesCount >= currentWave.EnemyQuantity)
        {
            NextWave(currentWave);
        }
    }

    private void SpawnEnemyForHackers(Enemy enemy, float multi)
    {
        var obj = Instantiate(enemy.Prefab, GetLessRandomSpawnPosition(), Quaternion.identity);
        obj.transform.SetParent(AllEnemies);
        var basicEnemy = obj.GetComponent<BasicEnemy>();
        basicEnemy.OnSendKilledDataToSpawner += OnEnemyKilled;

        if (IsDefaultWave)
        {
            basicEnemy.SetParams(enemy);
        }
        else
        {
            // scale
            var enem = enemy.ScaleParams();
            enem.HitPointsRange = new Vector2Int(Mathf.RoundToInt(enem.HitPointsRange.x * multi), Mathf.RoundToInt(enem.HitPointsRange.y * multi));
            basicEnemy.SetParams(enem);
            if (basicEnemy.IsShooter)
            {
                var shooting = basicEnemy as IShooter;
                if (shooting != null)
                {
                    shooting.ShootingCooldown *= ShootingSpeedFactor/ (multi * 1.2f);
                }
            }

            basicEnemy.GetToGameAreaTimeRange /= 2;
        }

        lastSpawnTime = Time.time;
    }

    private void SpawnEnemy(Enemy enemy)
    {
        var obj = Instantiate(enemy.Prefab, GetLessRandomSpawnPosition(), Quaternion.identity);
        obj.transform.SetParent(AllEnemies);
        var basicEnemy = obj.GetComponent<BasicEnemy>();
        basicEnemy.OnSendKilledDataToSpawner += OnEnemyKilled;

        if (IsDefaultWave)
            basicEnemy.SetParams(enemy);
        else
        {
            // scale
            basicEnemy.SetParams(enemy.ScaleParams());
            if (basicEnemy.IsShooter)
            {
                var shooting = basicEnemy as IShooter;
                if (shooting != null)
                {
                    shooting.ShootingCooldown *= ShootingSpeedFactor;
                }
            }
        }

        lastSpawnTime = Time.time;
    }

    private void NextWave(EnemyWave currentWave)
    {
        spawnedEnemiesCount = 0;
        // check if we should spawn miniboss
        if (waveNumber > 10 && (waveNumber + 1) % 5 == 0)
        {
            // spawn miniboss
            // destroy mines
            foreach (Transform mine in MinesHolder)
            {
                mine.GetComponent<Mine>().SilentExplosion();
            }

            if (MinesHolder.childCount > 0)
                GameEffectsPool.SpawnElectricExplosion(transform.position, 1.3f);

            SpawnMiniboss();

            AdvanceWaveNumber();
        }
        else
        {
            // go to next wave
            currentActiveWave = GetIndexForNextWave();
            var nextWave = Profile.Waves[currentActiveWave];
            CalculateSpawnTime(nextWave);

            //if (!Profile.Waves[currentActiveWave].IsSilent)
            //    GameManager.instance.EventManager.Dispatch(new NextWaveEvent(Profile.Waves[currentActiveWave]));
            var isSilent = Profile.Waves[currentActiveWave].IsSilent;
            GameManager.instance.EventManager.Dispatch(new NextWaveEvent(Profile.Waves[currentActiveWave], isSilent));
            AdvanceWaveNumber();

            // if above certain wave, start increasing number of enemies
            ScaleActiveEnemiesNumberCount(waveNumber);
        }
    }

    /// <summary>
    /// The single place the run's wave counter moves, so the HUD can never drift from the number
    /// the miniboss ladder, the difficulty curve and the spawn-gap floor all read (S203).
    ///
    /// The HUD used to display the wave ASSET's index instead. In the campaign that happens to be
    /// the wave number because the profile plays in order; in endless it is whichever of the six
    /// entries was drawn, so the counter jittered between 1 and 6 forever and a run that had
    /// reached wave 40 could show "2".
    /// </summary>
    private void AdvanceWaveNumber()
    {
        waveNumber++;

        if (GameManager.instance != null)
            GameManager.instance.EventManager.Dispatch(new RunWaveChangedEvent(waveNumber));
    }

    private List<Enemy> spawnedBosses = new List<Enemy>();
    private void SpawnMiniboss()
    {
        var unendingProfile = (UnendingWavesProfile)Profile;
        var chooseMinibosses = unendingProfile.Minibosses;

        //if(lastBoss)
        //{
        //    chooseMinibosses = chooseMinibosses.Where(x => x.Enemies[0].Prefab != lastBoss).ToList();
        //}

        //var enemy = chooseMinibosses.Random().GetMiniboss();
        //lastBoss = enemy.Prefab;
        Enemy enemy;
        var choosePool = chooseMinibosses.Where(x => !spawnedBosses.Contains(x.Enemies[0])).ToList();
        if (choosePool == null || choosePool.Count == 0)
        {
            spawnedBosses.Clear();
            enemy = chooseMinibosses.Random().GetMiniboss();
        }
        else
        {
            enemy = choosePool.Random().GetMiniboss();
        }

        spawnedBosses.Add(enemy);
            
        StartCoroutine(SpawnMinibossDelayed(enemy));

        isMinibossSpawned = true;
    }

    private IEnumerator SpawnMinibossDelayed(Enemy enemy)
    {
        yield return new WaitForSeconds(4);
        var obj = Instantiate(enemy.Prefab, BossSpawnPoint.position, Quaternion.identity);
        obj.transform.SetParent(AllEnemies);
        var bossEnemy = obj.GetComponentInChildren<BasicBoss>();
        bossEnemy.Initialize(this);
        bossEnemy.SetParams(enemy.ScaleParams());
    }

    private void ScaleActiveEnemiesNumberCount(int forWave)
    {
        // if above certain wave, start increasing number of enemies
        if (forWave > 10)
        {
            if (forWave % 3 == 0)
            {
                increaseNumberOfEnemies++;
            }

            if (forWave % 4 == 0)
            {
                SpawnRateFactor -= 0.06f;
                if (SpawnRateFactor < 0.4f)
                    SpawnRateFactor = 0.4f;
            }

            if (forWave % 2 == 0)
            {
                ShootingSpeedFactor -= 0.05f;
                if(ShootingSpeedFactor <= 0.4)
                {
                    ShootingSpeedFactor = 0.4f;
                }
            }
        }
    }

    private int upperCount, lowerCount;
    private Transform lastPosition;
    private Vector3 GetLessRandomSpawnPosition()
    {
        var toChoose = SpawnPositions.Where(x => x != lastPosition).ToList();
        
        if(upperCount > 2)
        {
            toChoose.RemoveAll(x => x.position.y > 0);
            upperCount = 0;
        }

        else if (lowerCount > 2)
        {
            toChoose.RemoveAll(x => x.position.y < 0);
            lowerCount = 0;
        }

        var rand = toChoose.Random();
        var pos = rand.position;
        lastPosition = rand;

        if (pos.y > 0)
            upperCount++;
        else lowerCount++;

        pos.x = transform.position.x;

        return pos;
    }

    private void CalculateSpawnTime(EnemyWave wave)
    {
        SpawnCooldown = Random.Range(wave.SpawnCooldownRange.x, wave.SpawnCooldownRange.y) * SpawnRateFactor;

        // Density floor (GDD 3.1). Deliberately endless-only: the campaign profile's pacing is
        // hand-authored and its long early gaps are intentional, whereas endless just replays six
        // waves forever and one of them (Index 9) has a spawn range topping out at 9.16s -- about
        // 10.8 enemies/minute against 20-25 for every other endless wave. That reads as the game
        // forgetting about you.
        if (!IsDefaultWave)
            SpawnCooldown = Mathf.Clamp(SpawnCooldown, MinSpawnGapSeconds, EndlessMaxSpawnGap());
    }

    // ---------------------------------------------------------------------
    // Endless wave selection (GDD 3.1, S202)
    //
    // Was: currentActiveWave = Random.Range(0, Profile.Waves.Count) -- uniform over every entry
    // in the profile, including entries with no spawnable enemy at all. Now: weighted by how
    // dense a wave actually is, never twice in a row, and unplayable entries are excluded rather
    // than relied on to fail gracefully.
    // ---------------------------------------------------------------------

    private const float MinSpawnGapSeconds = 0.15f;
    private const float EndlessMaxGapEarly = 3.0f;
    private const float EndlessMaxGapLate = 1.8f;

    private readonly List<int> endlessCandidates = new List<int>();
    private readonly List<float> endlessWeights = new List<float>();
    private float endlessWeightTotal;
    private int lastEndlessWave = -1;
    private bool endlessCandidatesBuilt;

    /// <summary>Longest tolerable silence between spawns, tightening from wave 10 to wave 30.</summary>
    private float EndlessMaxSpawnGap()
    {
        var t = Mathf.InverseLerp(10f, 30f, waveNumber);
        return Mathf.Lerp(EndlessMaxGapEarly, EndlessMaxGapLate, t);
    }

    /// <summary>
    /// A wave is playable only if it can actually put an enemy on the field. Checking the
    /// probability alone is not enough (a wave can hold a real prefab at probability 0) and
    /// checking the prefab alone is not enough either (GetEnemyRandomByProbability short-circuits
    /// to Enemies[0] when every probability is 0).
    /// </summary>
    private static bool IsWavePlayable(EnemyWave wave)
    {
        if (wave == null || wave.Enemies == null || wave.Enemies.Count == 0)
            return false;

        for (var i = 0; i < wave.Enemies.Count; i++)
        {
            var enemy = wave.Enemies[i];

            if (enemy != null && enemy.Prefab != null && enemy.ProbabilityInWave > 0f)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Expected enemies per minute at SpawnRateFactor 1, used as the selection weight.
    ///
    /// The zero-cooldown guard is not cosmetic: a wave authored with SpawnCooldownRange {0,0}
    /// yields 60/0 = infinity, and an infinite weight would make that one wave win every single
    /// draw. Treating it as the fastest allowed cadence keeps it finite and honest.
    /// </summary>
    private static float WaveDensityPerMinute(EnemyWave wave)
    {
        var meanCooldown = (wave.SpawnCooldownRange.x + wave.SpawnCooldownRange.y) * 0.5f;
        meanCooldown = Mathf.Max(meanCooldown, MinSpawnGapSeconds);

        return Mathf.Clamp(60f / meanCooldown, 1f, 60f);
    }

    private void RebuildEndlessCandidates()
    {
        endlessCandidates.Clear();
        endlessWeights.Clear();
        endlessWeightTotal = 0f;
        endlessCandidatesBuilt = true;

        if (Profile == null || Profile.Waves == null)
            return;

        for (var i = 0; i < Profile.Waves.Count; i++)
        {
            var wave = Profile.Waves[i];

            // Silent waves are scripted breathers -- they suppress the per-wave stat upgrade and
            // the difficulty tick -- so they must never come up in a random rotation. The endless
            // profile happens to have none today; this keeps that true if one is ever authored.
            if (wave == null || wave.IsSilent || !IsWavePlayable(wave))
                continue;

            var weight = WaveDensityPerMinute(wave);
            endlessCandidates.Add(i);
            endlessWeights.Add(weight);
            endlessWeightTotal += weight;
        }

        if (endlessCandidates.Count == 0)
            Debug.LogError("[EnemySpawner] The endless profile has no playable waves; falling back to index 0.");
    }

    private int PickEndlessWave()
    {
        if (!endlessCandidatesBuilt)
            RebuildEndlessCandidates();

        if (endlessCandidates.Count == 0)
            return 0;

        if (endlessCandidates.Count == 1)
            return endlessCandidates[0];

        var picked = DrawWeightedWave();

        // One redraw is enough to break back-to-back repeats without skewing the distribution
        // the way a reject-until-different loop would.
        if (picked == lastEndlessWave)
            picked = DrawWeightedWave();

        lastEndlessWave = picked;
        return picked;
    }

    private int DrawWeightedWave()
    {
        var roll = Random.value * endlessWeightTotal;

        for (var i = 0; i < endlessCandidates.Count; i++)
        {
            roll -= endlessWeights[i];

            if (roll <= 0f)
                return endlessCandidates[i];
        }

        // Float accumulation can leave a sliver at the top of the range.
        return endlessCandidates[endlessCandidates.Count - 1];
    }

    // the index is looped
    private int GetIndexForNextWave()
    {
        if (IsDefaultWave)
        {
            currentActiveWave++;

            if (currentActiveWave > Profile.Waves.Count - 1)
            {
                IsDefaultWave = false;
                Profile = UnendingProfile;
                RebuildEndlessCandidates();
                currentActiveWave = PickEndlessWave();
            }
        }
        else
        {
            currentActiveWave = PickEndlessWave();
        }

        return currentActiveWave;
    }

    internal void SpawnMine(Vector3 pos)
    {
        var mine = Instantiate(LevelProps.instance.MinePrefab, pos, Quaternion.identity);
        mine.transform.SetParent(MinesHolder);
    }

    internal void SpawnMine(float y)
    {
        var pos = GetLessRandomSpawnPosition();
        pos.y = y;
        SpawnMine(pos);
    }

    public void OnEnemyKilled(BasicEnemy enemy)
    {
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
    }

    // Boss accounting is asymmetric and deliberately left that way: a full boss increments
    // currentEnemyCount (Update's IsBoss branch falls through to the ++), a miniboss never does
    // (SpawnMinibossDelayed instantiates directly). So do NOT "fix" this by adding a blanket
    // decrement here -- it would subtract for minibosses that were never added, under-count the
    // field and over-spawn. The residual +1 per full boss is absorbed by ResyncEnemyCount().
    public void OnEnemyKilled(BasicBoss boss)
    {
        // boss killed, skip all
        spawnedEnemiesCount = 0;
        // go to next wave if not silent
        currentActiveWave = GetIndexForNextWave();

        CalculateSpawnTime(Profile.Waves[currentActiveWave]);

        var isSilent = Profile.Waves[currentActiveWave].IsSilent;
        GameManager.instance.EventManager.Dispatch(new NextWaveEvent(Profile.Waves[currentActiveWave], isSilent));

        isMinibossSpawned = false;
        isBossSpawned = false;
    }
}