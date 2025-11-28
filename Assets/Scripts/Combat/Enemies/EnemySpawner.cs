using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : TimeCompute
{
    public EnemyWavesProfile Profile;
    public EnemyWavesProfile UnendingProfile;

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

    internal void KillAllEnemies(GameObject except)
    {
        var killedAny = false;
        foreach (Transform enemy in AllEnemies)
        {
            if (enemy.gameObject == except.transform.parent.gameObject)
                continue;

            GameEffectsPool.SpawnNormalExplosionMuted(enemy.position, 1);
            enemy.gameObject.SetActive(false);
            killedAny = true;
        }

        if(killedAny)
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.GameOverSound, 0.6f);
    }

    internal void KillAllEnemies()
    {
        foreach (Transform enemy in AllEnemies)
        {
            GameEffectsPool.SpawnNormalExplosionMuted(enemy.position, 1);
            enemy.gameObject.SetActive(false);
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
            NextWave(currentWave);
            lastSpawnTime = Time.time;
        }
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
            waveNumber++;
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

            waveNumber++;
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
            waveNumber++;

            // if above certain wave, start increasing number of enemies
            ScaleActiveEnemiesNumberCount(waveNumber);
        }
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
    }

    // the index is looped
    private int GetIndexForNextWave()
    {
        if (IsDefaultWave)
        {
            currentActiveWave++;

            if (currentActiveWave > Profile.Waves.Count - 1)
            {
                currentActiveWave = 0;
                IsDefaultWave = false;
                Profile = UnendingProfile;
            }
        }
        else
        {
            currentActiveWave = Random.Range(0, Profile.Waves.Count);
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
        currentEnemyCount--;
    }

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