using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DebugTerminal.Runtime;
using UnityEngine;

namespace Datatypes
{
    public static class StatsTracker
    {
        private static Dictionary<string, StatsEntry> _entityPopulations = new Dictionary<string, StatsEntry>();
        public static Dictionary<string, StatsEntry> EntityPopulations => _entityPopulations;

        //How Frequently the logs are updated
        public static float logFrequency = 1f;
        public static bool _isLogging { get; private set; }
        private static int _updateRate;

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            Debug.Log("<b>StatsTracker</b> Initialized");
            StartLogging();
        }

        [RegisterCommand(Help = "Begins logging stats over time")]
        public static void StartLogging()
        {
            _isLogging = true;
            _updateRate = Mathf.RoundToInt(1000 * logFrequency);
            UpdateLog();
        }

        [RegisterCommand(Help = "Stops the logging of stats over time")]
        public static void StopLogging()
        {
            _isLogging = false;
            LogMessage("Stopped logging");
        }

        private static async Task UpdateLog()
        {
            while (_isLogging)
            {
                foreach (var entry in _entityPopulations.Values)
                {
                    entry.LogUpdate();
                }

                await Task.Delay(_updateRate);
            }
        }

        /// <summary>
        /// Called whenever a new entity is spawned
        /// </summary>
        /// <param name="id"></param>
        public static void AddEntity(string id)
        {
            if (!_entityPopulations.ContainsKey(id))
            {
                _entityPopulations.Add(id, new StatsEntry{
                    id = id,
                    count = 0,
                    countLog = new List<double>()
                });
            }

            _entityPopulations[id].Increase();
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

            _entityPopulations[id].Decrease();
        }

        /// <summary>
        /// Returns true if the state tacker contains the give state
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool ContainsId(string id)
        {
            return _entityPopulations.ContainsKey(id);
        }

        /// <summary>
        /// Returns true if all entities of a category have been killed off
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool EntityExtinct(string id)
        {
            if(!ContainsId(id))
            {
                return true;
            }

            return _entityPopulations[id].count <= 0;
        }

        /// <summary>
        /// Returns the <see cref="StatsEntry"/> with the given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static StatsEntry GetEntry(string id)
        {
            return _entityPopulations[id];
        }

        /// <summary>
        /// Will return true if the stats tracker contains the given entity id as well as the corresponding entry
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool TryGetEntry(string id, out StatsEntry entry)
        {
            entry = null;

            if (_entityPopulations.ContainsKey(id))
            {
                entry = _entityPopulations[id];
                return true;
            }

            return false;
        }

        public static void LogMessage(string message)
        {
            Debug.Log($"<b>StatsTracker:</b> {message}");
        }
    }

    public class StatsEntry
    {
        public string id;
        public double count;
        public List<double> countLog = new List<double>();
        public Dictionary<string, double> states = new Dictionary<string, double>();

        public void Increase()
        {
            count++;
        }

        public void StateChange(string originalState, string targetState)
        {
            if (states.ContainsKey(originalState))
            {
                states[originalState]--;
            }

            if (!states.ContainsKey(targetState))
            {
                states.Add(targetState, 0);
            }

            states[targetState]++;
        }

        public void Decrease()
        {
            count--;
        }

        public void LogUpdate()
        {
            countLog.Add(count);
        }
    }
}
