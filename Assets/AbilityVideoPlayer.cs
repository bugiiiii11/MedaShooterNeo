using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class AbilityVideoPlayer : MonoBehaviour
{
    public TMPro.TextMeshProUGUI TitleText, Description;
    public VideoPlayer Video;
    public GameObject Frames;
    public OpenLinkButton LinkButton;

    public void Close()
    {
        Frames.SetActive(false);
    }

    public void PreviewAbility(int id)
    {
        gameObject.SetActive(true);
        AbilityConfig heroAbility = null;
        LinkButton.Link = "https://cryptomeda.tech/marketplace";
        LinkButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Go To Market";
        switch (id)
        {
            case 0:
                TitleText.text = "Chain Gun";
                heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbility("Chain Gun");
                Description.text = heroAbility.AbilityDescription;
                Video.url = "https://dui1zywca5dp5.cloudfront.net/videos/chaingun.mp4";
                break;

            case 1:
                TitleText.text = "Deep Wound";
                heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbility("Deep Wound");
                Description.text = heroAbility.AbilityDescription;
                Video.url = "https://dui1zywca5dp5.cloudfront.net/videos/deepwound.mp4";
                break;

            case 2:
                TitleText.text = "Snap";
                heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbility("Snap");
                Description.text = heroAbility.AbilityDescription;
                Video.url = "https://dui1zywca5dp5.cloudfront.net/videos/snap.mp4";
                break;
            case 3:
                LinkButton.Link = "https://cryptomeda.tech/staking";
                LinkButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Stake";

                TitleText.text = "Shield";
                Description.text = "Generate a shield that will absorb all damage for 7 seconds.";
                Video.url = "https://dui1zywca5dp5.cloudfront.net/videos/shield_1.mp4";
                break;
        }
        Video.prepareCompleted += Video_prepareCompleted;
        Video.Prepare();
    }

    private void Video_prepareCompleted(VideoPlayer source)
    {
        Frames.SetActive(true);
        Video.Play();
    }
}
