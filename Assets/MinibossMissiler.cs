using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinibossMissiler : MinibossBase
{
    public enum State
    {
        GoToGameArea,
        Attack
    }
    public Vector3 GoToPosition;
    public GameObject HomingMissilePrefab;
    public List<State> StateMachine;
    private int currentState = 0;

    [Header("Combat")]
    public Vector2 AttackWithProjectilesCooldownRange;

    [Header("Movement")]
    public Vector2 MovementCooldownRange;
    private float lastChangeMovementTime = 0;
    private float currentMovementCooldown = 0;
    protected Vector3 CurrentPoint;

    private float lastAttackWithProjectilesTime = 0;
    private float currentAttackCooldown = 0;
    public Transform WeaponSocket;

    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
        {
            _animator.SetFloat("Speed", 0f);
            return;
        }

        if (currentHitPoints <= 0)
            return;

        var state = StateMachine[currentState];

        var playerPos = GameManager.instance.Player.transform.position + Vector3.up;

        AimAt(playerPos, 3);

        if (Time.time - lastAttackWithProjectilesTime > currentAttackCooldown)
        {
            currentAttackCooldown = AttackWithProjectilesCooldownRange.Random();
            lastAttackWithProjectilesTime = Time.time;

            var value = UnityEngine.Random.value;

            if (value < 0.2f)
                weaponController.TripleFire();
            else
                weaponController.MissileFire(HomingMissilePrefab, 0.35f);
        }
        else
        {
            weaponController.StopFire();
        }

        switch (state)
        {
            case State.GoToGameArea:
                _transform.position = Vector3.MoveTowards(_transform.position, GoToPosition, Time.deltaTime * 2);
                _animator.SetFloat("Speed", 2.2f);

                if (_transform.position == GoToPosition)
                {
                    NextState();
                }
                break;
            case State.Attack:
                //move

                _transform.position = Vector3.MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * 1.6f * GameConstants.Constants.GameSpeedMultiplier);
                if (_transform.position == CurrentPoint)
                    _animator.SetFloat("Speed", 0f);
                else
                    _animator.SetFloat("Speed", 2.2f);


                if (Time.time - lastChangeMovementTime > currentMovementCooldown)
                {
                    currentMovementCooldown = MovementCooldownRange.Random();
                    lastChangeMovementTime = Time.time;
                    ChangeDirection();
                }

                break;
        }
    }

    protected virtual void ChangeDirection()
    {
        var ll = LevelInfo.instance.BoundLowerLeft.position.y;
        var ur = LevelInfo.instance.BoundUpperRight.position.y;


        CurrentPoint = new Vector3(transform.position.x, UnityEngine.Random.Range(ll, ur));
    }

    private void NextState()
    {
        currentState = (currentState + 1) % StateMachine.Count;
    }

    public override void Initialize(EnemySpawner spawner)
    {
        base.Initialize(spawner);
        AttackWithProjectilesCooldownRange *= spawner.ShootingSpeedFactor;
    }


    private void AimAt(Vector3 aimPos, float speed)
    {
        // Look direction
        var dir = (aimPos - WeaponSocket.position).normalized;
        dir.z = 0;

        // Weapon socket to FX Socket offset
        var currentWeapon = weaponController.GetCurrentWeapon();
        var offset = currentWeapon.FXSocket.position - WeaponSocket.position;
        offset.z = 0;
        var localOffset = WeaponSocket.InverseTransformVector(offset);
        localOffset.x = 0;
        localOffset.z = 0;

        //  Debug.DrawLine(WeaponSocket.position, currentWeapon.FXSocket.position, Color.yellow);
        var worldOffset = WeaponSocket.TransformVector(localOffset) - WeaponSocket.right * 5 * Mathf.Sign(dir.x);
        var weaponDir = (aimPos - (WeaponSocket.position + worldOffset)).normalized;
        var socketRotation = Quaternion.LookRotation(Vector3.forward,
            Mathf.Sign(dir.x) * Vector3.Cross(Vector3.forward, weaponDir));
        WeaponSocket.rotation = Quaternion.Lerp(WeaponSocket.rotation, socketRotation, Time.deltaTime * speed);

        // Lock Weapon Socket Angle
        var rot = WeaponSocket.rotation;
        const float z = 0.35f;

        if (WeaponSocket.rotation.z > z)
        {
            rot.z = z;
            WeaponSocket.rotation = rot;
        }
    }
}
