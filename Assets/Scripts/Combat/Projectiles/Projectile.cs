using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Projectile : SpriteProjectile
{
    private ParticleSystem _pSystem;

    protected override void Awake() 
    {
        base.Awake();
        _pSystem = GetComponent<ParticleSystem>();
    }

    protected override void OnCollisionEnter2D(Collision2D other)
    {
        var contacts = new ContactPoint2D[2];
        var contactsLength = other.GetContacts(contacts);

        if (contactsLength > 0)
        {
            var contact = other.contacts[0];

            base.SpawnProjectileHit(other, contact);

            HandleDamage(contact.collider);

            if (_rBody != null && _collider != null)
            {
                _rBody.isKinematic = true;
                _rBody.velocity *= 0;
                _collider.enabled = false;

                _rBody.simulated = false;
            }

            _pSystem.Stop(true);
            _pSystem.Clear(true);

            // despawn
            Destroy(gameObject, 0.4f);
        }
    }
}