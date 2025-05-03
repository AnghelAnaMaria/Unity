using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class NeighbourStrategyFactory
    {
        //maps the lowercase class-name of each IFindNeighbourStrategy implementation to its Type
        Dictionary<string, Type> strategies;

        public NeighbourStrategyFactory()
        {
            LoadTypesIFindNeighbourStrategy();
        }

        private void LoadTypesIFindNeighbourStrategy()
        {
            strategies = new Dictionary<string, Type>();

            // Get all types in this assembly
            Type[] typesInThisAssembly = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in typesInThisAssembly)
            {
                // If this type implements IFindNeighbourStrategy, register it
                if (type.GetInterface(typeof(IFindNeighbourStrategy).ToString()) != null)
                {
                    strategies.Add(type.Name.ToLower(), type);
                }
            }
        }

        internal IFindNeighbourStrategy CreateInstance(string nameOfStrategy)
        {
            // try requested strategy
            var t = GetTypeToCreate(nameOfStrategy);
            // fallback to "more" if not found
            if (t == null)
                t = GetTypeToCreate("more");
            // instantiate via reflection
            return Activator.CreateInstance(t) as IFindNeighbourStrategy;
        }

        private Type GetTypeToCreate(string nameOfStrategy)
        {
            foreach (var possibleStrategy in strategies)
            {
                // match if the registered key contains the requested substring
                if (possibleStrategy.Key.Contains(nameOfStrategy))
                    return possibleStrategy.Value;
            }

            return null;
        }

    }
}

