using UnityEngine;

namespace ReneVerse
{
    public class BannerAdSurface: AdSurfaceBase
    {
        public override async void ServeAd()
        {
            _serveAdData ??= await ReneAPIManager.ServeAd(_adSurface.AdSurfaceId);
            if (_serveAdData == null) return;
            base.ServeAd();
            StartCoroutine(Helper.LoadTextureFromURLAndSettingMeshRenderer
                (_serveAdData.Url, adMesh));
        }

        private void OnDrawGizmos()
        {
            DrawGizmoQuad(Color.green, adMesh,Constants.BannerGizmo);
        }
    }
}