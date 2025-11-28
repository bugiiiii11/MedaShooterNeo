using ReneVerse;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VideoAdUi : VideoAdSurface
{
    public GameObject holderObject;

    public override void ServeAd()
    {
        holderObject.SetActive(true);
        base.ServeAd();
    }

    public override void DisableAd()
    {
        holderObject.SetActive(false);
        base.DisableAd();
    }

    public new void DrawGizmoQuad(Color gizmoColor, MeshRenderer referencedModel, string gizmoIcon = null)
    { }

    [ContextMenu("Copy")]
    public void CopyFromVideoAdSurface()
    {
        var comps = GetComponents<VideoAdSurface>();
        VideoAdSurface compToCopyFrom = null;
        if (comps[0] is VideoAdUi vau)
        {
            compToCopyFrom = comps[1];
        }
        else if (comps[1] is VideoAdUi vau2)
        {
            compToCopyFrom = comps[0];
        }

        this._adSurface.AdSurfaceId = compToCopyFrom._adSurface.AdSurfaceId;
        this._adSurface.AdType = compToCopyFrom._adSurface.AdType;
        this._adSurface.Interactivity = compToCopyFrom._adSurface.Interactivity;
        this._adSurface.ResolutionIab = compToCopyFrom._adSurface.ResolutionIab;
    }
}
