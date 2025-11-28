using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace ReneVerse
{
    public class VideoAdSurface : AdSurfaceBase
    {
        [SerializeField] private VideoPlayer _videoPlayer;

        public override async void ServeAd()
        {
            _serveAdData ??= await ReneAPIManager.ServeAd(_adSurface.AdSurfaceId);
            if (_serveAdData == null) return; // Early return if no ad data is received
            base.ServeAd();
            _videoPlayer.url = _serveAdData.Url;
            _videoPlayer.Play();
        }

        public override void DisableAd()
        {
            base.DisableAd();
            if (_videoPlayer) _videoPlayer.Stop();
        }

#if UNITY_EDITOR


        private void OnDrawGizmos()
        {
            DrawGizmoQuad(Color.blue, adMesh, Constants.VideoGizmo);

            if (TryGetComponent(out AudioSource audioSource))
            {
                Gizmos.color = Color.red; // Change this to any color you prefer
                Gizmos.DrawWireSphere(transform.position, audioSource.maxDistance);
            }

            Handles.Label(transform.position, _adSurface.AdSurfaceId);
        }
#endif
    }
}