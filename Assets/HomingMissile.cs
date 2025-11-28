using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : SpriteProjectile
{
	protected Transform target;
	protected Vector2 targetPosition => (Vector2)target.position + Vector2.up;
	public Vector2 Lifetime = new Vector2(1f, 3);
	public float speed = 5f;
	public float rotateSpeed = 200f;

	[NonSerialized]
	public float Drag = 1;
	protected Rigidbody2D rb;

	// Use this for initialization
	protected virtual void Start()
	{
		target = GameManager.instance.Player.transform;
		rb = GetComponent<Rigidbody2D>();

		Destroy(gameObject, Lifetime.Random());
	}

	void FixedUpdate()
	{
		Vector2 direction = targetPosition - rb.position;

		direction.Normalize();

		float rotateAmount = Vector3.Cross(direction, transform.up).z;
		rb.angularVelocity = -rotateAmount * rotateSpeed* Drag;
		rb.velocity = transform.up * speed;
	}

	public void ChangeDrag(float value, float afterTime)
    {
		StartCoroutine(ChangeDragCo(value, afterTime));
    }

    private IEnumerator ChangeDragCo(float value, float afterTime)
    {
		yield return new WaitForSeconds(afterTime);
		Drag = value;
    }
}