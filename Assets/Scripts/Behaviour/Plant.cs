using Behaviour;
using UnityEngine;

public class Plant : LivingEntity
{
    private readonly float amountMultiplier = 10;
    private float amountRemaining = 1;

    public float Consume(float amount)
    {
        var amountConsumed = Mathf.Max(0, Mathf.Min(amountRemaining, amount));
        amountRemaining -= amount * amountMultiplier;

        transform.localScale = Vector3.one * amountRemaining;

        if (amountRemaining <= 0 && !dead)
        {
            Environments.Environment.RegisterPlantDeath(this);
            dead = true;
            Destroy(gameObject);
        }

        return amountConsumed;
    }
}