using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityShotAnimator
{
    public string AbilityName;
    public RuntimeAnimatorController Controller;
}

public class AbilitySwitcher : MonoBehaviour
{
    public List<OhShitButtonActivator> Buttons;
    public List<AbilityShotAnimator> AbilityControllers;

    public WeaponButtonActivator WeaponButton;

    public RectTransform ContentForButtons;

    [Header("Editor Testing")]
    [Tooltip("Enable this to force-enable shield in Editor without NFT check")]
    public bool ForceEnableShieldInEditor = false;

    private OhShitButtonActivator activeAbility = null;
    private bool shieldEnabled = false;

    // Singleton instance for external access
    public static AbilitySwitcher Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to the land ticket check completion event
        InventoryBackend.OnLandTicketCheckComplete += OnLandTicketCheckComplete;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        InventoryBackend.OnLandTicketCheckComplete -= OnLandTicketCheckComplete;
    }

    /// <summary>
    /// Called when land ticket ownership check completes (from InventoryBackend)
    /// </summary>
    private void OnLandTicketCheckComplete(bool isOwner)
    {
        Debug.Log($"🛡️ AbilitySwitcher received land ticket check result: isOwner = {isOwner}");
        if (isOwner && !shieldEnabled)
        {
            EnableShieldAbility();
        }
    }

    private void Start()
    {
        var ppi = PlayerProfileInfo.instance;

        Debug.Log($"🛡️ AbilitySwitcher.Start() called");
        Debug.Log($"🛡️ PlayerProfileInfo.instance exists: {ppi != null}");
        Debug.Log($"🛡️ Buttons list count: {Buttons?.Count ?? 0}");

#if UNITY_EDITOR
        // Editor testing: force enable shield without NFT check
        if (ForceEnableShieldInEditor)
        {
            Debug.Log("🛡️ EDITOR TEST MODE: Force-enabling shield ability");
            EnableShieldAbility();
        }
#endif

        if (ppi)
        {
            // Shield ability is enabled when user owns NFT Land Ticket
            Debug.Log($"🛡️ AbilitySwitcher.Start() - IsLandTicketOwner: {ppi.IsLandTicketOwner}");
            Debug.Log($"🛡️ AbilitySwitcher.Start() - WalletAddress: {ppi.WalletAddress}");

            if (ppi.IsLandTicketOwner)
            {
                Debug.Log("🛡️ IsLandTicketOwner is TRUE - enabling shield immediately");
                EnableShieldAbility();
            }
            else if (!shieldEnabled) // Don't start delayed check if already enabled by editor toggle
            {
                // If not set yet, start a coroutine to check again after a short delay
                // This handles the case where the async land ticket check hasn't completed yet
                Debug.Log("🛡️ IsLandTicketOwner is FALSE - starting delayed checks");
                StartCoroutine(DelayedLandTicketCheck());
            }

            if (ppi.EquippedHero != null)
            {
                var equippedAbility = ppi.NftHandler.GetAbilityForFraction(((NftHero)ppi.EquippedHero).Fraction);
                if (equippedAbility != null)
                {
                    var ability = Buttons.FirstOrDefault(x => x.AbilityConfig.AbilityName == equippedAbility.AbilityName);
                    ability.gameObject.SetActive(true);
                    ability.transform.SetParent(ContentForButtons);
                    activeAbility = ability;
                }
            }
        }

        if (ppi && ppi.EquippedWeapon != null)
        {
            if (WeaponButton)
            {
                WeaponButton.gameObject.SetActive(true);
                WeaponButton.weaponName = ppi.EquippedWeapon.Name;
                WeaponButton.SetIcon(WeaponButton.weaponName);
                WeaponButton.transform.SetParent(ContentForButtons);
            }
        }
    }

    /// <summary>
    /// Delayed check for land ticket ownership in case the async check was still in progress
    /// </summary>
    private IEnumerator DelayedLandTicketCheck()
    {
        var ppi = PlayerProfileInfo.instance;

        // Check multiple times over several seconds to handle async timing
        float[] checkDelays = { 0.5f, 1f, 2f, 3f, 5f };

        foreach (float delay in checkDelays)
        {
            yield return new WaitForSeconds(delay);

            if (shieldEnabled) yield break; // Already enabled, stop checking

            ppi = PlayerProfileInfo.instance;
            Debug.Log($"🛡️ Delayed check (after {delay}s): IsLandTicketOwner = {ppi?.IsLandTicketOwner}");

            if (ppi && ppi.IsLandTicketOwner)
            {
                Debug.Log($"🛡️ Delayed land ticket check - enabling shield after {delay}s");
                EnableShieldAbility();
                yield break;
            }
        }

        Debug.Log("🛡️ All delayed checks completed - shield not enabled (user doesn't own land ticket)");
    }

    /// <summary>
    /// Enable the shield ability button - can be called externally if land ticket check completes after Start()
    /// </summary>
    public void EnableShieldAbility()
    {
        if (shieldEnabled) return;

        var shield = Buttons.FirstOrDefault(x => x.AbilityConfig.AbilityName == "Shield");
        if (shield != null)
        {
            shield.gameObject.SetActive(true);
            shield.transform.SetParent(ContentForButtons);

            // Set shield duration based on NFT Land count
            float shieldDuration = InventoryBackend.GetShieldDuration(PlayerProfileInfo.instance.NftLandCount);
            shield.AbilityConfig.ActivatedTime = shieldDuration;
            Debug.Log($"🛡️ Shield duration set to {shieldDuration} seconds (NFT Land count: {PlayerProfileInfo.instance.NftLandCount})");

            // Ensure the icon is set correctly
            shield.SetIcon();

            shieldEnabled = true;
            Debug.Log("🛡️ Shield ability ENABLED in game");
        }
        else
        {
            Debug.LogWarning("🛡️ Shield ability button not found in Buttons list!");
        }
    }

    internal void SetAbilityCooldownFactor(float cooldownReductionFactor)
    {
        activeAbility.AbilityConfig.Cooldown = Mathf.RoundToInt(cooldownReductionFactor * activeAbility.AbilityConfig.Cooldown);
    }
}
