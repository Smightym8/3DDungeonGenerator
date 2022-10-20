using Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;

    public void ActivateAttacking()
    {
        if (maxHealth <= 0) return;

        if (gameObject.tag.Equals(Tag.Enemy))
        {
            GetComponent<EnemyMovement>().GotHit();
        }
    }

    private void Update()
    {
        if (maxHealth <= 0)
        {
            maxHealth = 0;

            if (gameObject.tag.Equals(Tag.Player))
            {
                SceneManager.LoadScene(0);
            }
            else if (gameObject.tag.Equals(Tag.Enemy))
            {
                GetComponent<EnemyMovement>().Die();
                Destroy(gameObject, 5);
            }
        }
    }
}
