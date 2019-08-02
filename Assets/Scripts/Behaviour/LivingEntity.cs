using System;
using Datatypes;
using UnityEngine;

namespace Behaviour
{
    public class LivingEntity : MonoBehaviour
    {
        public Coord coord;

        public string id;

        protected bool dead;

        [Header("Settings")]
        [SerializeField] private float decayTimer = 10;

        [HideInInspector]
        public Coord mapCoord;

        [HideInInspector]
        public int mapIndex;

        //Callback for
        public Action<string> onDeath;
        public Action onDecayed;

        public virtual void Init(Coord coord)
        {
            this.coord = coord;
            transform.position = Environments.Environment.tileCentres[coord.x, coord.y];
        }

        public virtual void Update()
        {
            if (dead)
            {
                decayTimer -= Time.deltaTime;
                if (decayTimer <= 0)
                {
                    onDecayed?.Invoke();
                    Destroy(gameObject);
                }
            }
        }

        public virtual void Die(string reason = "Natural Causes")
        {
            dead = true;
            onDeath?.Invoke(reason);
            StatsTracker.RemoveEntity(id);
        }
    }
}