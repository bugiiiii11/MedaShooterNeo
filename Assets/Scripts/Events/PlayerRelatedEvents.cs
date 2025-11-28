public class PlayerHealthChangeEvent
{
    public int CurrentHp, MaxHp;

    public PlayerHealthChangeEvent(int currHp, int maxHp)
    {
        CurrentHp = currHp;
        MaxHp = maxHp;
    }

    public PlayerHealthChangeEvent(PlayerStatsModule stats)
    {
        CurrentHp = stats.CurrentHp;
        MaxHp = stats.MaxHp;
    }
}

public class PlayerDamagedEvent
{

}

public class WeaponPowerupTimedEvent
{
    public int Time;

    public WeaponPowerupTimedEvent(int time)
    {
        Time = time;
    }
}

public class PlayerArmorChangeEvent
{
    public int CurrentArmor, MaxArmor;

    public PlayerArmorChangeEvent(int currArmor, int maxArmor)
    {
        CurrentArmor = currArmor;
        MaxArmor = maxArmor;
    }
}

public class PlayerCurrenciesChangeEvent
{
    public PlayerCurrencyModule CurrentCurrencies;

    public PlayerCurrenciesChangeEvent(PlayerCurrencyModule currencies)
    {
        CurrentCurrencies = currencies;
    }
}

public class GamePauseEvent
{
    public bool IsPaused;
    public GamePauseEvent(bool paused)
    {
        IsPaused = paused;
    }
}

public class PlayerDiedEvent
{
    public PlayerDiedEvent()
    {
    }
}

public class PerkActivatedEvent
{
    public bool IsStack;
    public Perk Perk;
    public PerkBehaviour Behaviour;
    public PerkActivatedEvent(Perk perk, PerkBehaviour behaviour, bool isStack)
    {
        Perk = perk;
        IsStack = isStack;
        Behaviour = behaviour;
    }
}

public class PerkDeactivatedEvent
{
    public bool IsStack;
    public PerkBehaviour Perk;

    public PerkDeactivatedEvent(PerkBehaviour perk, bool isStack)
    {
        Perk = perk;
        IsStack = isStack;
    }
}

public class PowerupCollectedEvent
{
    public IngamePowerup Powerup;

    public PowerupCollectedEvent(IngamePowerup p)
    {
        Powerup = p;
    }
}

public class DroppableCollectedEvent
{
    public Droppable Drop;

    public DroppableCollectedEvent(Droppable d)
    {
        Drop = d;
    }
}

public class PerkSelectionEvent
{
    public int Count;
    public Rarity[] AllowedRarities;
    public string OverrideTitle = "<b>Choose your perk!</b>";
    public bool IsBuildup;

    public PerkSelectionEvent(int count)
    {
        Count = count;
        AllowedRarities = new[] { Rarity.Basic, Rarity.Epic, Rarity.Rare };
        IsBuildup = false;
    }

    public PerkSelectionEvent(int count, Rarity[] allowedRarities)
    {
        Count = count;
        AllowedRarities = allowedRarities;
        IsBuildup = false;
    }

    public PerkSelectionEvent(int count, string overrideTitle, Rarity[] allowedRarities)
    {
        Count = count;
        AllowedRarities = allowedRarities;
        OverrideTitle = overrideTitle;
        IsBuildup = false;
    }

    public PerkSelectionEvent(int count, string overrideTitle)
    {
        Count = count;
        AllowedRarities = new[] { Rarity.Basic, Rarity.Epic, Rarity.Rare };
        OverrideTitle = overrideTitle;
        IsBuildup = false;
    }

    public PerkSelectionEvent(int count, string overrideTitle, bool isBuildup)
    {
        Count = count;
        AllowedRarities = new[] { Rarity.Basic, Rarity.Epic, Rarity.Rare };
        OverrideTitle = overrideTitle;
        IsBuildup = isBuildup;
    }
}
