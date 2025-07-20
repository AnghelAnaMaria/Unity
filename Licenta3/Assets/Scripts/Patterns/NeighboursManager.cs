// MIT License
// 
// Copyright (c) 2020 Sunny Valley Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Modified by: Anghel Ana-Maria, iulie 2025 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace WaveFunctionCollapse
{//Clasa care alege o strategie din cele 2
 //Pas 1: prima oara (in WFC) cream obiectul NeighbourStrategyFactory deci apelam LoadTypesIFindNeighbourStrategy() -> avem in strategies ca Types: NeighbourStrategySize1Default si NeighboursStrategySize2OrMore (adica 2 strategii posibile)
 //Pas 2 : dupa (in WFC workflow) apelam CreateInstance(sring conventie) ca sa cream o instanta pt un obiect (ori de Type NeighbourStrategySize1Default ori de Type NeighboursStrategySize2OrMore) -> ramanem cu un singur Type (adica o singura strategie la rulare)
    public class NeighboursManager
    {
        private Dictionary<string, Type> strategies;//un dicționar (nume clasa, Type clasa)


        //Pas 1:
        public NeighboursManager()
        {
            LoadTypesIFindNeighbourStrategy();
        }

        private void LoadTypesIFindNeighbourStrategy()
        {
            strategies = new Dictionary<string, Type>();

            //obținem toate tipurile din assembly-ul curent (de la toate clasele din proiect adica)
            Type[] typesInThisAssembly = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in typesInThisAssembly)
            {
                //dacă tipul implementează IFindNeighbourStrategy(adica daca avem NeighbourStrategySize1Default sau NeighboursStrategySize2OrMore)
                if (type.GetInterface(typeof(INeighbours).ToString()) != null)
                {
                    //îl înregistrăm sub cheia numelui clasei, în lowercase
                    strategies.Add(type.Name.ToLower(), type);
                }
            }
        }

        //Pas 2:
        internal INeighbours CreateInstance(string nameOfStrategy)//nameOfStrategy= numele cu care apelez eu in WFC Algorythm
        {
            Type typeForStrategy = GetTypeToCreate(nameOfStrategy);//Type pt numele nameOfStrategy

            if (typeForStrategy == null)
                typeForStrategy = GetTypeToCreate("more");//adica luam Type=Neighbours2More

            return Activator.CreateInstance(typeForStrategy) as INeighbours;//Creează dinamic un obiect al clasei NeighbourStrategySize1Default sau NeighboursStrategySize2OrMore (ADICA O STRATEGIE)
        }

        private Type GetTypeToCreate(string nameOfStrategy)//nameOfStrategy= numele cu care apelez eu in WFC Algorythm
        {
            foreach (var possibleStrategy in strategies)//pt fiecare pereche (string, Type) in strategies
            {
                //ex: daca apelam cu nameOfStrategy="size1" programul o sa stie ca vreau valoarea Type=NeighbourStrategySize1Default
                //ex: daca apelam cu nameOfStrategy="size2" programul o sa stie ca vreau valoarea Type=NeighboursStrategySize2OrMore
                if (possibleStrategy.Key.Contains(nameOfStrategy))
                    return possibleStrategy.Value;//returnam Type-ul
            }

            return null;
        }

    }
}

