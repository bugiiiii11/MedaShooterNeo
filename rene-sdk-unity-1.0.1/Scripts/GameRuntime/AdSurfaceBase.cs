using Rene.Sdk.Api.Game.Data;
using ReneVerse;
using UnityEngine;

namespace ReneVerse
{

    [HelpURL(Constants.AdSurfaceDocsURL)]
    public abstract class AdSurfaceBase : MonoBehaviour
    {
        [HideInInspector] public AdSurfacesResponse.AdSurfacesData.AdSurface _adSurface;
        protected ServeAdResponse.ServeAdData _serveAdData;
        public bool IsAdServing { get; protected set; }

        [SerializeField] protected MeshRenderer adMesh;

        public virtual void ServeAd()
        {
            IsAdServing = true;
            adMesh.gameObject.SetActive(true);
        }

        public virtual void DisableAd()
        {
            if (IsAdServing)
            {
                IsAdServing = false;
                adMesh.gameObject.SetActive(false);
            }
        }

        protected void DrawGizmoQuad(Color gizmoColor, MeshRenderer referencedModel, string gizmoIcon = null)
        {
            // Using local scale since the quad is a child of the GameObject
            Vector3 localScale = referencedModel.transform.localScale;

            // Adjust the points based on local scale
            Vector3 topLeft = transform.TransformPoint(new Vector3(-0.5f * localScale.x, 0.5f * localScale.y, 0));
            Vector3 topRight = transform.TransformPoint(new Vector3(0.5f * localScale.x, 0.5f * localScale.y, 0));
            Vector3 bottomLeft = transform.TransformPoint(new Vector3(-0.5f * localScale.x, -0.5f * localScale.y, 0));
            Vector3 bottomRight = transform.TransformPoint(new Vector3(0.5f * localScale.x, -0.5f * localScale.y, 0));

            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // Calculate the center of the quad
            Vector3 center = referencedModel.transform.position;
            Gizmos.DrawIcon(center, gizmoIcon, true);
        }
    }
}