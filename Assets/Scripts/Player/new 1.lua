Hothead

public class EnemyTracker
{
    public BasicEnemy enemy;
    public Vector2Int BaseDamage;
    public TimeVariable UpgradeCounter = 2;
    public int TimesUpgraded = 0;
}


 var tracker = new EnemyTracker();
 tracker.UpgradeCounter.Reset();
 tracker.enemy = enemy;
 myList.Add(tracker);



.....v update vo foreach

if (tracker.UpgradeCounter.IsOver()
{
    var shooting = tracker.enemy.GetComponent<EnemyShooting>();

   // toto by som priradil az pri prvom ticku (aby doslo k upgradu enemy hp atd pri vyssich Mythicoch)
   if (tracker.TimesUpgraded == 0)
        tracker.BaseDamage = shooting.CurrentSetup.DamageRange;

   tracker.TimesUpgraded ++;
   tracker.UpgradeCounter.Reset();
   
   // toto asi nepojde lebo to je Vector2Int takze to treba prepocitat cez zaokruhlovanie po jednotlivych zlozkach X a Y
   shooting.DamageRange *= IncreaseDamagePercentage * tracker.TimesUpgraded;
}
