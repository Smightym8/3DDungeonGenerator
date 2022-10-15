using System;
using Enemy;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;

    public void ActivateAttacking()
    {
        if (gameObject.tag.Equals(Tag.Enemy))
        {
            GetComponent<EnemyMovement>().StartAttacking();
        }
    }
    
}
