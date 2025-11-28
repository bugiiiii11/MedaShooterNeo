using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cryptomeda.Minigames.BackendComs;
using System;
using Cryptomeda.NFT.Json;
using Newtonsoft.Json;
using System.Linq;
using Unity.Jobs.LowLevel.Unsafe;

public class InventoryBackend : MonoBehaviour
{
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
            if (data != null && data.Standard != null && data.Standard.CurrentlyStaked.HasValue && data.Liquidity != null && data.Liquidity.CurrentlyStaked.HasValue)
            {
                PlayerProfileInfo.instance.IsUserStaker = data.Standard.CurrentlyStaked > 0 || data.Liquidity?.CurrentlyStaked > 0;
                PlayerProfileInfo.instance.IsUserFarmer = data.Liquidity?.CurrentlyStaked > 0;

                if (PlayerProfileInfo.instance.IsUserStaker)
                {
                    UIInventory.instance.StakingAbility.SetActive(true);
                }
                else
                {
                    UIInventory.instance.StakingAbility.SetActive(false);
                }

                if (PlayerProfileInfo.instance.IsUserFarmer)
                {
                    UIInventory.instance.FarmingAbility.SetActive(true);
                }
                else
                {
                    UIInventory.instance.FarmingAbility.SetActive(false);
                }
            }
            else
            {
                UIInventory.instance.StakingAbility.SetActive(false);
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
}