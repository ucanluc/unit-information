using System;
using System.Collections;
using System.Collections.Generic;
using MapMono;
using UnityEngine;

public class UnitMarker : MonoBehaviour
{
    public bool friendly;
    public bool visible;
    public bool selected;
    public Renderer renderer;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    public void UpdateMaterial(bool friendly, bool visible, bool selected, MapSettings mapSettings)
    {
        if (this.friendly == friendly && this.visible == visible && this.selected == selected)
        {
            return;
        }


        this.friendly = friendly;
        this.visible = visible;
        this.selected = selected;

        if (selected)
        {
            renderer.material = mapSettings.selectedMarkerMaterial;
            return;
        }
        else if (friendly)
        {
            if (visible)
            {
                renderer.material = mapSettings.friendlyMarkerMaterial;
            }
            else
            {
                renderer.material = mapSettings.friendlyMarkerMaterialHidden;
            }
        }
        else
        {
            if (visible)
            {
                renderer.material = mapSettings.enemyMarkerMaterial;
            }
            else
            {
                renderer.material = mapSettings.enemyMarkerMaterialHidden;
            }
        }
    }
}