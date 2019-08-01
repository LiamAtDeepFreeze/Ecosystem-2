using System.Collections.Generic;
using UnityEngine;

namespace Datatypes
{
    public static class StatsTracker
    {
        private static Dictionary<string, double> _entityPopulations = new Dictionary<string, double>();
        public static Dictionary<string, double> EntityPopulations => _entityPopulations;

        /// <summary>
        /// Called whenever a new entity is spawned
        /// </summary>
        /// <param name="id"></param>
        public static void AddEntity(string id)
        {
            if (!_entityPopulations.ContainsKey(id))
            {
                _entityPopulations.Add(id, 0);
            }

            _entityPopulations[id]++;
        }

        /// <summary>
        /// Called whenever the entity dies
        /// </summary>
        /// <param name="id"></param>
        public static void RemoveEntity(string id)
        {
            if (!_entityPopulations.ContainsKey(id))
            {
                return;
            }

            _entityPopulations[id]--;
        }
    }
}
