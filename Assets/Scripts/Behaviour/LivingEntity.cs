using Datatypes;
using UnityEngine;

namespace Behaviour
{
    public class LivingEntity : MonoBehaviour
    {
        public Coord coord;

        protected bool dead;

        [HideInInspector]
        public Coord mapCoord;

        [HideInInspector]
        public int mapIndex;

        public virtual void Init(Coord coord)
        {
            this.coord = coord;
            transform.position = Environments.Environment.tileCentres[coord.x, coord.y];
        }
    }
}