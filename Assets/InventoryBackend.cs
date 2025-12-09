using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cryptomeda.Minigames.BackendComs;
using System;
using Cryptomeda.NFT.Json;
using Newtonsoft.Json;
using System.Linq;
using Unity.Jobs.LowLevel.Unsafe;

public class InventoryBackend : MonoBehaviour
{
    // NFT Land Ticket contract address on Polygon
    private const string LAND_TICKET_CONTRACT = "0xAAE02c81133d865D543Df02b1e458de2279C4a5b";

    public static Dictionary<string, string> WeaponNames = new Dictionary<string, string>
    {
        { "11", "Gladiator's Greatsword" },
        { "12", "Ryoshi Katana" },
        { "13", "Tactician's Claymore" },
        { "14", "Blessed Blade" },
        { "21", "Viper" },
        { "22", "Underdog Meda-gun" },
        { "23", "Adept's Repeater" },
        { "24", "Sandcrawler's Sniper Rifle" },
    };

    public bool RunOnStart = true;
    private void Start()
    {
        Debug.Log($"🔍 DEBUG: InventoryBackend.Start() called, RunOnStart = {RunOnStart}");

        if (RunOnStart)
        {
            // 🔧 Check if PlayerProfileInfo.instance exists first
            if (PlayerProfileInfo.instance == null)
            {
                Debug.LogWarning("⚠️ PlayerProfileInfo.instance is NULL! Creating a temporary one for testing...");

                // Create a temporary GameObject with PlayerProfileInfo for testing
                GameObject tempPlayerProfile = new GameObject("TempPlayerProfileInfo");
                tempPlayerProfile.AddComponent<PlayerProfileInfo>();

                Debug.Log("✅ Created temporary PlayerProfileInfo instance");
            }

            var addr = PlayerProfileInfo.instance.WalletAddress;
            Debug.Log($"🔍 DEBUG: PlayerProfileInfo.instance = NOT NULL");
            Debug.Log($"🔍 DEBUG: Initial WalletAddress = '{addr}' (is null/empty: {string.IsNullOrWhiteSpace(addr)})");

            // 🔧 EDITOR TESTING: Use hardcoded wallet for testing
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(addr))
            {
                // addr = "0x32A7C95DC89D5AB912522337Fc0DB673F32514B5"; // Test wallet
                 addr = "0xA5e82D9C3d80B4dDB93766874A3c13c19eb3Da54"; // Test wallet
                // addr = "0x9A87D7a9A537C4eBCA4442036Bd20Dc14821DE90"; // Test wallet
                PlayerProfileInfo.instance.WalletAddress = addr;
                Debug.Log($"🔧 EDITOR: Using test wallet address: {addr}");
            }
#endif

            if (string.IsNullOrWhiteSpace(addr) || string.IsNullOrEmpty(addr))
            {
                Debug.Log("Address is null or empty, retrieving from cache...");

                // cached address
                if (PlayerPrefs.HasKey("cached_addr_data"))
                {
                    Debug.Log("Found a cached address, getting data from it...");
                    var cachedAddr = PlayerPrefs.GetString("cached_addr_data");
                    addr = cachedAddr;
                    Debug.Log("Retrieved cached data: " + addr);

                    // update cached address
                    PlayerProfileInfo.instance.WalletAddress = addr;
                }
            }

            Debug.Log($"🔍 DEBUG: Final wallet address to use: '{addr}'");

            //GetData<NftInventory>(RestfulEndpoint.UserNfts, "0x3c03b473c5c9c0055e6863d6fe148eb3850482de", OnReceivedNfts);
            GetInventory(addr);
        }
    }

    public void GetInventoryFor(TMPro.TMP_InputField field)
    {
        GetInventory(field.text);
    }
    public void GetInventoryFrom(TMPro.TMP_InputField json)
    {
        var response = new Response();
        response.Text = json.text;
        response.Code = 201;
        OnReceivedData<NftWeaponInventory[]>(response, OnReceivedWeapons);
    }

    public void GetInventory(string address)
    {
        Debug.Log("Fetch inventory for " + address);

        PlayerProfileInfo.instance.NftWeapons = InventoryConfig.Default;
        PlayerProfileInfo.instance.NftWeapons.Type = InventoryConfig.ConfigType.Weapons;
        PlayerProfileInfo.instance.NftWeapons.Name = "Weapons";

        PlayerProfileInfo.instance.NftHeroes = InventoryConfig.Default;
        PlayerProfileInfo.instance.NftHeroes.Type = InventoryConfig.ConfigType.Heores;
        PlayerProfileInfo.instance.NftHeroes.Name = "Heroes";

        PlayerProfileInfo.instance.BoostPackages = new List<BoostPackage>();

        /*UICardPreview.instance.Unequip();
        UICardPreview.instance.UnequipWeapon();
        UICardPreview.instance.UnequipBoosts();*/

        GetData<NftInventory>(RestfulEndpoint.UserNfts, address, OnReceivedNfts);
        GetData<StakingInfo>(RestfulEndpoint.Staking, address, (data) =>
        {
            Debug.Log($"🎫 Staking data received: {(data != null ? "valid" : "null")}");

            if (data != null)
            {
                long standardStaked = data.Standard?.CurrentlyStaked ?? 0;
                long liquidityStaked = data.Liquidity?.CurrentlyStaked ?? 0;
                long totalStaked = standardStaked + liquidityStaked;

                Debug.Log($"🎫 Standard staked: {standardStaked}, Liquidity staked: {liquidityStaked}, Total: {totalStaked}");

                PlayerProfileInfo.instance.IsUserStaker = totalStaked > 0;
                PlayerProfileInfo.instance.IsUserFarmer = liquidityStaked > 0;
                PlayerProfileInfo.instance.NftLandCount = totalStaked;

                // NFT Land ownership is determined by staking data
                // If user has any staked amount > 0, they own NFT Land
                bool hasNftLand = totalStaked > 0;
                PlayerProfileInfo.instance.IsLandTicketOwner = hasNftLand;

                Debug.Log($"🎫 NFT Land ownership result: IsOwner = {hasNftLand}, Count = {totalStaked}");

                // Update shield sprite based on NFT Land ownership
                UpdateShieldSprite(hasNftLand);

                // Notify AbilitySwitcher that land ticket check is complete (with shield duration)
                NotifyLandTicketCheckComplete(hasNftLand);

                // Update farming ability visibility
                if (UIInventory.instance != null && UIInventory.instance.FarmingAbility != null)
                {
                    UIInventory.instance.FarmingAbility.SetActive(PlayerProfileInfo.instance.IsUserFarmer);
                }
            }
            else
            {
                Debug.LogWarning("🎫 Staking data is null - defaulting to no NFT Land");
                PlayerProfileInfo.instance.IsLandTicketOwner = false;
                PlayerProfileInfo.instance.NftLandCount = 0;
                UpdateShieldSprite(false);
            }
        });
        GetData<NftWeaponInventory[]>(RestfulEndpoint.UserWeapons, address, OnReceivedWeapons);
        // GetData<List<BoostPackage>>(RestfulEndpoint.BoostPackages, address, OnReceivedBoostPackages);
    }

    private void OnReceivedBoostPackages(List<BoostPackage> obj)
    {
        if (obj == null)
            return;

        foreach (var boost in obj)
        {
            print($"got id {boost.Id} with abilities({boost.Abilities.Count}): {boost.Abilities.Select(x => x.GameId).Stringify()}");
        }
    }

    private void OnReceivedWeapons(NftWeaponInventory[] obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("⚔️ WARNING: Received null weapons data");
            return;
        }

        Debug.Log($"⚔️ DEBUG: Processing {obj.Length} weapons from API");

        foreach (var weapon in obj)
        {
            if (!weapon.Minted)
            {
                Debug.Log($"⚔️ DEBUG: Skipping unminted weapon: {weapon.WeaponName}");
                continue;
            }

            var nftWeapon = new NftWeapon(weapon.Security.HasValue ? (int)weapon.Security : 0,
                weapon.Anonymity.HasValue ? (int)weapon.Anonymity : 0,
                weapon.Innovation.HasValue ? (int)weapon.Innovation : 0,
                0,
                weapon.WeaponName);

            var (_, tier) = WeaponButtonActivator.GetDetieredWeaponNameWithTier(nftWeapon.Name);
            if (tier == "T2")
            {
                // boost T2 weapon by 30%
                nftWeapon.Boost(0.3f);
                Debug.Log($"⚔️ DEBUG: Boosted T2 weapon: {nftWeapon.Name}");
            }
            else if (tier != "T1")
            {
                Debug.Log($"⚔️ DEBUG: Skipping weapon with invalid tier: {nftWeapon.Name} (tier: {tier})");
                continue;
            }

            PlayerProfileInfo.instance.NftWeapons.Add(nftWeapon);
            Debug.Log($"⚔️ DEBUG: Added weapon: {nftWeapon.Name} (Sec:{nftWeapon.Security}, Ano:{nftWeapon.Anonymity}, Inn:{nftWeapon.Innovation})");
        }

        Debug.Log($"⚔️ DEBUG: Total weapons after processing: {PlayerProfileInfo.instance.NftWeapons.Nfts.Count}");

        // 🔥 FIX: DISPLAY WEAPONS AFTER THEY'RE PROCESSED
        if (UIInventory.instance != null)
        {
            Debug.Log("⚔️ DEBUG: Calling UIInventory.instance.DisplayWeapons() from OnReceivedWeapons");
            UIInventory.instance.DisplayWeapons();
        }
        else
        {
            Debug.LogError("⚔️ ERROR: UIInventory.instance is null!");
        }

        /*
        var nftWeap = new NftWeapon(12, 70, 68, 0, "Viper");
        PlayerProfileInfo.instance.NftWeapons.Add(nftWeap);*/
    }

    // Enhanced OnReceivedNfts method with comprehensive debugging
    private void OnReceivedNfts(NftInventory obj)
    {
        Debug.Log("📡 DEBUG: OnReceivedNfts called");

        if (obj == null)
        {
            Debug.LogError("❌ ERROR: Received null NftInventory object");
            return;
        }

        Debug.Log($"📡 DEBUG: NftInventory received - Count: {obj.Count}, Results: {obj.Results?.Count ?? 0}");

        if (obj.Results == null)
        {
            Debug.LogWarning("⚠️ WARNING: NftInventory.Results is null");
            return;
        }

        if (obj.Results.Count == 0)
        {
            Debug.LogWarning("⚠️ WARNING: NftInventory.Results is empty");
            return;
        }

        // Log first few heroes for inspection
        Debug.Log("📡 DEBUG: First few heroes in response:");
        for (int i = 0; i < Math.Min(3, obj.Results.Count); i++)
        {
            var hero = obj.Results[i];
            Debug.Log($"   Hero {i}: ID={hero.Id}, Title='{hero.Title}', Owner='{hero.Owner}'");
            Debug.Log($"   Hero {i} Metadata: Sec={hero.Metadata?.Sec}, Ano={hero.Metadata?.Ano}, Inn={hero.Metadata?.Inn}");
            Debug.Log($"   Hero {i} Reward: Power={hero.Reward?.Power}");
        }

        // Start the builder
        StartCoroutine(NftBuilder(obj));
    }

    // Enhanced NftBuilder with comprehensive error handling
    private IEnumerator NftBuilder(NftInventory root)
    {
        Debug.Log("🔄 DEBUG: NftBuilder started");

        NftInventory next = null;
        List<NftInventory> loadedData = new List<NftInventory>();

        if (root != null)
        {
            Debug.Log($"🔄 DEBUG: Root inventory has {root.Results?.Count ?? 0} results");

            var url = root.Next;
            if (url != null)
            {
                Debug.Log($"🔄 DEBUG: Processing pagination, starting with: {url.OriginalString}");
                while (true)
                {
                    GetNftData(url.OriginalString, (data) => next = data);
                    yield return new WaitUntil(() => next != null);
                    Debug.Log($"🔄 DEBUG: Fetched page with {next.Results?.Count ?? 0} results");
                    loadedData.Add(next);
                    if (next.Next == null)
                        break;
                    else url = next.Next;

                    next = null;
                }
            }

            // Connect the data
            foreach (var item in loadedData)
            {
                if (item.Results != null)
                {
                    root.Results.AddRange(item.Results);
                    Debug.Log($"🔄 DEBUG: Added {item.Results.Count} results from pagination");
                }
            }

            Debug.Log($"🦸 DEBUG: Processing {root.Results?.Count ?? 0} heroes from API (after pagination)");

            // Enhanced hero processing with individual error handling
            int processedCount = 0;
            int successCount = 0;
            int errorCount = 0;

            if (root.Results != null)
            {
                foreach (var item in root.Results)
                {
                    processedCount++;
                    Debug.Log($"🦸 DEBUG: Processing hero #{processedCount}: ID={item.Id}, Title='{item.Title}'");

                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrEmpty(item.Title))
                        {
                            Debug.LogWarning($"⚠️ WARNING: Hero #{processedCount} has empty title, skipping");
                            errorCount++;
                            continue;
                        }

                        // Check metadata with null safety
                        if (item.Metadata == null)
                        {
                            Debug.LogWarning($"⚠️ WARNING: Hero #{processedCount} ({item.Title}) has null metadata, using defaults");
                        }

                        // Check reward with null safety
                        if (item.Reward == null)
                        {
                            Debug.LogWarning($"⚠️ WARNING: Hero #{processedCount} ({item.Title}) has null reward, using defaults");
                        }

                        // Check fraction field
                        var fractionStr = item.Fraction.ToString();
                        Debug.Log($"🦸 DEBUG: Hero #{processedCount} fraction: '{fractionStr}'");

                        var hero = new NftHero(
                            (int)(item.Metadata?.Sec ?? 0),
                            (int)(item.Metadata?.Ano ?? 0),
                            (int)(item.Metadata?.Inn ?? 0),
                            (int)(item.Reward?.Power ?? 0),
                            item.Metadata?.Revolution == true ? "Neutral" : fractionStr,
                            item.Title,
                            item.Metadata?.Revolution ?? false);

                        hero.OwnerWallet = item.Owner;
                        PlayerProfileInfo.instance.NftHeroes.Add(hero);

                        successCount++;
                        Debug.Log($"✅ SUCCESS: Hero #{processedCount} ({item.Title}) added successfully - Sec:{hero.Attributes.Security}, Ano:{hero.Attributes.Anonymity}, Inn:{hero.Attributes.Innovation}, Pwr:{hero.Attributes.Power}");
                    }
                    catch (System.Exception ex)
                    {
                        errorCount++;
                        Debug.LogError($"❌ ERROR: Failed to process hero #{processedCount} ({item.Title}): {ex.Message}");
                        Debug.LogError($"❌ ERROR: Stack trace: {ex.StackTrace}");

                        // Log detailed item data for problematic hero
                        Debug.LogError($"❌ ERROR: Item details - ID:{item.Id}, Owner:{item.Owner}, Fraction:{item.Fraction}");
                        if (item.Metadata != null)
                        {
                            Debug.LogError($"❌ ERROR: Metadata - Sec:{item.Metadata.Sec}, Ano:{item.Metadata.Ano}, Inn:{item.Metadata.Inn}, Revolution:{item.Metadata.Revolution}");
                        }
                        else
                        {
                            Debug.LogError($"❌ ERROR: Metadata is NULL");
                        }

                        if (item.Reward != null)
                        {
                            Debug.LogError($"❌ ERROR: Reward - Power:{item.Reward.Power}");
                        }
                        else
                        {
                            Debug.LogError($"❌ ERROR: Reward is NULL");
                        }

                        // Continue processing other heroes instead of stopping
                        continue;
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ WARNING: root.Results is null");
            }

            Debug.Log($"🦸 SUMMARY: Processed {processedCount} heroes - {successCount} successful, {errorCount} errors");
        }
        else
        {
            Debug.LogError("❌ ERROR: Root NftInventory is null");
        }

        Debug.Log($"🦸 DEBUG: Total heroes after processing: {PlayerProfileInfo.instance.NftHeroes?.Nfts?.Count ?? 0}");

        // Always try to display heroes, even if some failed
        if (UIInventory.instance != null)
        {
            Debug.Log("🦸 DEBUG: Calling UIInventory.instance.DisplayHeroes() from NftBuilder");
            UIInventory.instance.DisplayHeroes();
        }
        else
        {
            Debug.LogError("🦸 ERROR: UIInventory.instance is null!");
        }
    }

    //#################################################################################

    public void GetNftData(string url, Action<NftInventory> callback)
    {
        RestfulManager.GetFromUrl(url, (response) => OnReceivedData(response, callback));
    }

    public void GetData<TObject>(RestfulEndpoint endpoint, string address, Action<TObject> callback)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.Log("Address is null or empty, can't fetch any data");
            callback(default);
            return;
        }

        RestfulManager.GetByUrlParam(endpoint, $"?address={address}", (response) =>
        {
            if (response.Code >= 400)
            {
                Debug.Log(response.Text + ": " + endpoint.ToString());
                OnReceivedData(response, callback);
            }
            else
                OnReceivedData(response, callback);
        });
    }

    private void OnReceivedData<TObject>(Response obj, Action<TObject> callback)
    {
        try
        {
            var root = JsonConvert.DeserializeObject<TObject>(obj.Text);
            callback(root);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"JSON Deserialization Error: {e.Message}");
            Debug.LogWarning($"Response Text: {obj.Text?.Substring(0, Math.Min(500, obj.Text.Length))}...");
            callback(default);
        }
    }

    #region NFT Land Ticket Check

    /// <summary>
    /// Check if wallet owns any NFT Land Tickets using Polygon RPC
    /// Contract: 0xAAE02c81133d865D543Df02b1e458de2279C4a5b
    /// </summary>
    public void CheckLandTicketOwnership(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("🎫 Cannot check land ticket: wallet address is empty");
            PlayerProfileInfo.instance.IsLandTicketOwner = false;
            return;
        }

        StartCoroutine(CheckLandTicketBalanceCoroutine(walletAddress));
    }

    private IEnumerator CheckLandTicketBalanceCoroutine(string walletAddress)
    {
        Debug.Log($"🎫 Checking NFT Land Ticket ownership for: {walletAddress}");

        // Use Polygon RPC to call balanceOf on the ERC-721 contract
        // balanceOf(address) function selector: 0x70a08231
        string paddedAddress = walletAddress.ToLower().Replace("0x", "").PadLeft(64, '0');
        string data = "0x70a08231" + paddedAddress;

        // JSON-RPC request body
        var rpcRequest = new
        {
            jsonrpc = "2.0",
            method = "eth_call",
            @params = new object[]
            {
                new { to = LAND_TICKET_CONTRACT, data = data },
                "latest"
            },
            id = 1
        };

        string jsonBody = JsonConvert.SerializeObject(rpcRequest);

        // Use public Polygon RPC endpoint
        string polygonRpc = "https://polygon-rpc.com";

        using var request = new UnityWebRequest(polygonRpc, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<RpcResponse>(request.downloadHandler.text);
                if (response != null && !string.IsNullOrEmpty(response.result))
                {
                    // Parse hex result to get balance
                    string hexBalance = response.result;
                    Debug.Log($"🎫 Raw hex balance response: {hexBalance}");

                    bool hasBalance = false;

                    if (hexBalance.StartsWith("0x"))
                    {
                        // Remove 0x prefix and leading zeros
                        string hexValue = hexBalance.Substring(2).TrimStart('0');

                        // If empty after trimming, balance is 0
                        if (string.IsNullOrEmpty(hexValue))
                        {
                            hasBalance = false;
                            Debug.Log("🎫 Balance is 0 (empty hex value)");
                        }
                        else
                        {
                            // Any non-zero value means they own at least one NFT
                            hasBalance = true;
                            Debug.Log($"🎫 Non-zero balance detected: 0x{hexValue}");
                        }
                    }

                    PlayerProfileInfo.instance.IsLandTicketOwner = hasBalance;
                    Debug.Log($"🎫 Land Ticket ownership result: IsOwner = {PlayerProfileInfo.instance.IsLandTicketOwner}");

                    // Update UI based on land ticket ownership (for shield ability)
                    // Show bright shield sprite if user owns NFT Land, "Staking Tech" sprite otherwise
                    if (UIInventory.instance != null)
                    {
                        UIInventory.instance.StakingAbility.SetActive(true);
                        var shieldImage = UIInventory.instance.StakingAbility.GetComponent<UnityEngine.UI.Image>();
                        if (shieldImage != null)
                        {
                            // Swap sprite based on NFT Land ownership
                            if (PlayerProfileInfo.instance.IsLandTicketOwner)
                            {
                                // User owns NFT Land - show bright shield sprite
                                if (UIInventory.instance.ShieldSprite != null)
                                {
                                    shieldImage.sprite = UIInventory.instance.ShieldSprite;
                                }
                            }
                            else
                            {
                                // User doesn't own NFT Land - show "Staking Tech" sprite
                                if (UIInventory.instance.StakingTechSprite != null)
                                {
                                    shieldImage.sprite = UIInventory.instance.StakingTechSprite;
                                }
                            }
                            shieldImage.color = Color.white; // Always full brightness
                        }
                    }

                    // Notify AbilitySwitcher that land ticket check is complete
                    NotifyLandTicketCheckComplete(hasBalance);
                }
                else
                {
                    Debug.LogWarning($"🎫 Invalid RPC response: {request.downloadHandler.text}");
                    PlayerProfileInfo.instance.IsLandTicketOwner = false;
                    SetStakingAbilityGreyed();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"🎫 Error parsing land ticket response: {e.Message}");
                PlayerProfileInfo.instance.IsLandTicketOwner = false;
                SetStakingAbilityGreyed();
            }
        }
        else
        {
            Debug.LogError($"🎫 Failed to check land ticket: {request.error}");
            PlayerProfileInfo.instance.IsLandTicketOwner = false;
            SetStakingAbilityGreyed();
        }
    }

    [Serializable]
    private class RpcResponse
    {
        public string jsonrpc;
        public int id;
        public string result;
        public RpcError error;
    }

    [Serializable]
    private class RpcError
    {
        public int code;
        public string message;
    }

    /// <summary>
    /// Updates the shield button sprite based on NFT Land ownership
    /// </summary>
    private void UpdateShieldSprite(bool hasNftLand)
    {
        if (UIInventory.instance != null && UIInventory.instance.StakingAbility != null)
        {
            UIInventory.instance.StakingAbility.SetActive(true);
            var shieldImage = UIInventory.instance.StakingAbility.GetComponent<UnityEngine.UI.Image>();
            if (shieldImage != null)
            {
                if (hasNftLand)
                {
                    // User owns NFT Land - show blue shield sprite
                    if (UIInventory.instance.ShieldSprite != null)
                    {
                        shieldImage.sprite = UIInventory.instance.ShieldSprite;
                        Debug.Log("🛡️ Shield sprite set to BLUE (user owns NFT Land)");
                    }
                }
                else
                {
                    // User doesn't own NFT Land - show grey shield sprite
                    if (UIInventory.instance.StakingTechSprite != null)
                    {
                        shieldImage.sprite = UIInventory.instance.StakingTechSprite;
                        Debug.Log("🛡️ Shield sprite set to GREY (user doesn't own NFT Land)");
                    }
                }
                shieldImage.color = Color.white; // Full brightness
            }
        }
    }

    /// <summary>
    /// Helper to show staking ability with "Staking Tech" sprite (user doesn't own NFT Land)
    /// </summary>
    private void SetStakingAbilityGreyed()
    {
        UpdateShieldSprite(false);
    }

    /// <summary>
    /// Notifies AbilitySwitcher when land ticket check completes (works across scenes)
    /// </summary>
    private void NotifyLandTicketCheckComplete(bool isOwner)
    {
        Debug.Log($"🎫 Notifying land ticket check complete: isOwner = {isOwner}");

        // If AbilitySwitcher exists (we're in gameplay scene), enable shield immediately
        if (AbilitySwitcher.Instance != null && isOwner)
        {
            Debug.Log("🎫 AbilitySwitcher found, enabling shield ability now");
            AbilitySwitcher.Instance.EnableShieldAbility();
        }

        // Also invoke the static event for any other listeners
        OnLandTicketCheckComplete?.Invoke(isOwner);
    }

    /// <summary>
    /// Static event that fires when land ticket ownership check completes.
    /// Subscribe to this in AbilitySwitcher to handle async completion.
    /// </summary>
    public static event Action<bool> OnLandTicketCheckComplete;

    /// <summary>
    /// Calculates shield duration based on NFT Land count
    /// 1-4: 7 seconds, 5-9: 8 seconds, 10-14: 9 seconds, 15+: 10 seconds
    /// </summary>
    public static float GetShieldDuration(long nftLandCount)
    {
        if (nftLandCount >= 15)
            return 10f;
        else if (nftLandCount >= 10)
            return 9f;
        else if (nftLandCount >= 5)
            return 8f;
        else if (nftLandCount >= 1)
            return 7f;
        else
            return 7f; // Default
    }

    #endregion
}