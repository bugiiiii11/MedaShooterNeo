using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraHomingMissile : HomingMissile
{
    private float lifetime = 1, currentLifetime = 0;
    public GameObject HydraProjectiles;

    protected override void Start()
    {
        target = GameManager.instance.Player.transform;
        rb = GetComponent<Rigidbody2D>();
        lifetime = Lifetime.Random();
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        currentLifetime += Time.deltaTime;

        if(currentLifetime >= lifetime / 2f)
        {
            currentLifetime = 0;
            Split();
            Destroy(gameObject);
        }
    }

    private void Split()
    {
        var hydra1 = Instantiate(HydraProjectiles, transform.position, transform.rotation).GetComponent<HomingMissile>();
        var hydra2 = Instantiate(HydraProjectiles, transform.position, transform.rotation).GetComponent<HomingMissile>();

        hydra1.Data = hydra2.Data = Data;
        //hydra1.Drag = UnityEngine.Random.Range(1.53f, 2f);
        //hydra2.Drag = -(UnityEngine.Random.Range(1.5f, 2f));

        hydra1.Drag = 1.5f;//UnityEngine.Random.Range(0.5f, 1f);
        hydra2.Drag = -1.5f; //-(UnityEngine.Random.Range(0.5f, 2f));

        var wait = UnityEngine.Random.Range(0.6f, 0.85f);

        hydra1.ChangeDrag(1, wait);
        hydra2.ChangeDrag(1, wait);
    }
}
