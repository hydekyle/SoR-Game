﻿using System;
using System.ComponentModel;
using System.Linq;
using Assets.FantasyHeroes.Scripts;
using UnityEngine;

[Serializable]
public struct Stats
{
    public int velocity, strength, life;
}

public enum BodyLimb { Head, LegLeft, LegRight, ArmLeft, ArmRight }

[Serializable]
public abstract class Entity
{
    public Character Dummy;
    public Transform transform;
    public Stats stats;
    public Animator AttackAnimator;
    public Rigidbody2D rigidbody;
    public LayerMask enemyMask;
    public string name;
    public int health = 9;
    public float lastTimeAttack = 0f;
    public float attackCD = 0.3f;
    public bool isActive = false;

    Transform headT, armLeftT, armRightT, legLeftT, legRightT;
    BoxCollider2D triggerCollider;
    BoxCollider2D rigidCollider;

    public abstract void GetStrike(int strikeForce, Vector2 hitDir);

    public void Start()
    {
        Transform pelvisT = transform.Find("Animation").Find("Pelvis");
        triggerCollider = pelvisT.GetComponent<BoxCollider2D>();
        rigidCollider = transform.GetComponent<BoxCollider2D>();
        Transform torsoT = pelvisT.Find("Torso");
        headT = torsoT.Find("Head");
        armLeftT = torsoT.Find("ArmL");
        armRightT = torsoT.Find("ArmR");
        legLeftT = pelvisT.Find("LegL");
        legRightT = pelvisT.Find("LegR");

    }

    public abstract void Update();

    public void Attack(Vector2 attackDir)
    {
        if (IsAttackAvailable())
        {
            rigidbody.AddForce(attackDir.normalized * 5, ForceMode2D.Impulse);
            lastTimeAttack = Time.time;
            PlayAnim("Attack");
            SlashAnim();
        }
    }

    public void ShootWeapon()
    {
        GameManager.Instance.ShootWeapon(transform.position, transform.right);
    }

    public void CastAttack()
    {
        var raycastHit = Physics2D.CircleCastAll(transform.position + transform.right / 2, 0.4f, transform.right, 0.9f);
        foreach (var hit in raycastHit)
        {
            LayerMask hitLayer = hit.transform.gameObject.layer;
            Vector2 hitDir = (hit.transform.position - transform.position).normalized;

            if (hitLayer == LayerMask.NameToLayer("Breakable"))
            {
                GameManager.BreakBreakable(hit.transform, hitDir);
            }
            else if (hitLayer == enemyMask)
            {
                if (this.GetType() == typeof(Player))
                {
                    Entity enemy = GameManager.Instance.GetEnemyByName(hit.transform.name);
                    if (enemy != null) StrikeEntity(enemy, hitDir);
                }
                else
                {
                    StrikeEntity(GameManager.Instance.player, hitDir);
                }
            }
            else if (hitLayer == LayerMask.NameToLayer("Movible"))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                hit.transform.GetComponent<Rigidbody2D>()?.AddForce(hitDir * 200 * stats.strength / Mathf.Pow(distance, 3), ForceMode2D.Impulse);
            }

        }
    }

    public void Die()
    {
        triggerCollider.enabled = false;
        transform.gameObject.layer = LayerMask.NameToLayer("Ghost");
        PlayAnim("Die");
    }

    public void ThrowBomb()
    {
        GameObject.Instantiate(GameManager.Instance.bombPrefab, transform.position, transform.rotation);
    }

    public void Burst(Vector2 hitDir)
    {
        Dismember(BodyLimb.ArmLeft, hitDir);
        Dismember(BodyLimb.ArmRight, hitDir);
        Dismember(BodyLimb.LegLeft, hitDir);
        Dismember(BodyLimb.LegRight, hitDir);
        Dismember(BodyLimb.Head, hitDir);
        Die();
    }

    public void Dismember(BodyLimb limb, Vector2 hitDir)
    {
        Transform oldLimb;
        switch (limb)
        {
            case BodyLimb.Head:
                oldLimb = headT;
                break;
            case BodyLimb.ArmRight:
                oldLimb = armRightT;
                break;
            case BodyLimb.ArmLeft:
                oldLimb = armLeftT;
                break;
            case BodyLimb.LegLeft:
                oldLimb = legLeftT;
                break;
            case BodyLimb.LegRight:
                oldLimb = legRightT;
                break;
            default:
                oldLimb = headT;
                break;
        }
        GameObject newGO = GameObject.Instantiate(oldLimb.gameObject);
        Transform newLimb = newGO.transform;
        oldLimb.gameObject.SetActive(false);

        newLimb.position = oldLimb.position;
        newLimb.localScale = oldLimb.lossyScale;
        newLimb.rotation = oldLimb.rotation;
        Rigidbody2D rb = newLimb.gameObject.AddComponent<Rigidbody2D>();
        newGO.SetActive(true);

        if (limb == BodyLimb.Head)
        {
            rb.gravityScale = 10f;
            rb.AddForce(Vector2.up * 10 + hitDir * 5, ForceMode2D.Impulse);

            // Poner la cabeza y sus elementos siempre visible
            SpriteRenderer parentRenderer = newLimb.GetComponent<SpriteRenderer>();
            parentRenderer.sortingOrder = parentRenderer.sortingOrder + 9999;
            foreach (Transform child in newLimb)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = renderer.sortingOrder + 9999;
                }
            }
        }
        else
        {
            rb.gravityScale = UnityEngine.Random.Range(5f, 10f);
            float force = UnityEngine.Random.Range(5f, 10f);
            rb.AddForce(Vector2.up * 10 + hitDir * force, ForceMode2D.Impulse);
        }
        CleanMyself(newLimb);
    }

    public void StrikeEntity(Entity entity, Vector2 hitDir)
    {
        entity.GetStrike(stats.strength * 10, hitDir);
    }

    public void CleanMyself(Transform limb)
    {
        GameManager.RemoveEntity(this);
        GameObject.Destroy(transform.gameObject, 2.5f);
        GameObject.Destroy(limb.gameObject, 2.5f);
    }

    public void SetAnimVelocity(int newVelocity)
    {
        Dummy.Animator.speed = newVelocity;
        AttackAnimator.speed = newVelocity;
    }

    public void PlayAnim(string clipName)
    {
        if (clipName == "Alert") SetAnimVelocity(1);
        else SetAnimVelocity(2);
        Dummy.Animator.StopPlayback();
        Dummy.Animator.Play(GameManager.GetAnimationName(clipName, Dummy.WeaponType));
    }

    public void SlashAnim()
    {
        AttackAnimator.Play("Slash1", 0);
    }

    public void MoveToDirection(Vector2 direction)
    {
        if (!IsPlayerAttacking())
        {
            PlayAnim("Walk");
            transform.position = Vector2.Lerp(
                GetActualPosition(),
                GetActualPosition() + direction * 2,
                Time.deltaTime * stats.velocity
            );

            if (Mathf.Abs(direction.x) > 0.0f)
            {
                SetOrientation(direction.x);
                CameraController.Instance.ZoomOption(ZoomOptions.Normal);
            }
        }
    }

    bool IsAttackAvailable()
    {
        return Time.time > lastTimeAttack + attackCD;
    }

    bool IsPlayerAttacking()
    {
        return Dummy.Animator.GetCurrentAnimatorStateInfo(0).IsName(GameManager.GetAnimationName("Attack", Dummy.WeaponType));
    }

    public void Idle()
    {
        if (!IsPlayerAttacking() && IsAttackAvailable()) PlayAnim("Alert");
    }

    public void ClampMyself(bool clampX, bool clampY)
    {
        transform.position = new Vector2(
            clampX ? Mathf.Clamp(transform.position.x, CameraController.Instance.maxPlayerDistanceLeft, Mathf.Infinity) : transform.position.x,
            clampY ? Mathf.Clamp(transform.position.y, -2.77f, 1f) : transform.position.y // HardCoded map boundaries :D
        );
    }

    private void SetOrientation(float directionX)
    {
        bool facingRight = directionX > 0 ? true : false;
        transform.rotation = Quaternion.AngleAxis(facingRight ? 0 : -180, Vector3.up);
    }

    private float GetCD()
    {
        return Mathf.Clamp(Time.time - lastTimeAttack + attackCD, 0f, 1f);
    }

    private Vector2 GetActualPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }

}

[Serializable]
public class Player : Entity
{
    public Player(Transform transform, Stats stats, string name)
    {
        this.transform = transform;
        this.stats = stats;
        this.name = name;
        this.Dummy = transform.GetComponent<Character>();
        this.AttackAnimator = transform.Find("Attack_Effect").GetComponent<Animator>();
        this.rigidbody = transform.GetComponent<Rigidbody2D>();
        this.SetAnimVelocity(2);
        this.enemyMask = LayerMask.NameToLayer("Enemy");
        this.isActive = true;
    }

    public override void Update()
    {
        if (isActive) ClampMyself(true, true);
    }

    public override void GetStrike(int strikeForce, Vector2 hitDir)
    {
        rigidbody.AddForce(hitDir.normalized * strikeForce, ForceMode2D.Impulse);
        health -= strikeForce;
        if (health > 0)
        {
            Debug.Log("Me hacen pupa");
        }
        else
        {
            Debug.Log("Legends never die");
        }
    }
}

[Serializable]
public class Enemy : Entity
{
    public Enemy(Transform transform, Stats stats, string name)
    {
        this.transform = transform;
        this.stats = stats;
        this.name = name;
        this.Dummy = transform.GetComponent<Character>();
        this.AttackAnimator = transform.Find("Attack_Effect").GetComponent<Animator>();
        this.rigidbody = transform.GetComponent<Rigidbody2D>();
        this.SetAnimVelocity(1);
        this.enemyMask = LayerMask.NameToLayer("Player");
    }

    public override void Update()
    {
        ClampMyself(false, true);
    }

    public override void GetStrike(int strikeForce, Vector2 hitDir)
    {
        rigidbody.AddForce(hitDir.normalized * strikeForce, ForceMode2D.Impulse);
        health -= strikeForce;
        if (health > 0)
        {
            // No muero
        }
        else
        {
            Burst(hitDir);
        }
    }
}