using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public static class RoomGroupGenerator
{
    // Clasa internă pentru șablon (template) – reprezintă o combinație dorită
    private class RoomGroupTemplate
    {
        public List<RoomType> Types { get; private set; }
        public RoomGroupTemplate(List<RoomType> types)
        {
            Types = types;
        }
    }


    /// Generează grupurile de camere pe baza unor șabloane de configurații.
    public static List<List<Room>> GenerateRoomGroups(List<Room> rooms)
    {
        // 1. Creăm pool-uri (cozi) pentru fiecare tip
        Dictionary<RoomType, Queue<Room>> pools = new Dictionary<RoomType, Queue<Room>>();
        foreach (RoomType rt in RoomType.GetValues(typeof(RoomType)))
        {
            pools[rt] = new Queue<Room>();
        }
        foreach (var room in rooms)
        {
            pools[room.GetRoomType()].Enqueue(room);
        }

        // 2. Definim șabloanele de grup, în ordinea de prioritate dorită.
        List<RoomGroupTemplate> templates = new List<RoomGroupTemplate>
    {
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Birou, RoomType.Baie}),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Birou, RoomType.Sufragerie}),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie, RoomType.Bucatarie, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie, RoomType.Birou, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Dormitor, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Dormitor, RoomType.Birou }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor, RoomType.Birou }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Birou, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie, RoomType.Bucatarie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie, RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie, RoomType.Birou }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Birou }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Baie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Dormitor }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Bucatarie }),
        new RoomGroupTemplate(new List<RoomType> { RoomType.Sufragerie })
    };

        // 3. Formăm grupurile conform șabloanelor, repetând procesul cât timp se poate forma vreun grup.
        List<List<Room>> groups = new List<List<Room>>();
        bool formedGroup = true;
        while (formedGroup)
        {
            formedGroup = false;
            // Amestecăm șabloanele pentru randomizare
            List<RoomGroupTemplate> shuffledTemplates = templates.OrderBy(t => UnityEngine.Random.value).ToList();

            // Grupăm șabloanele după numărul de elemente și le sortăm descrescător
            // (în interiorul fiecărui grup, le amestecăm random)
            List<RoomGroupTemplate> sortedTemplates = shuffledTemplates
                .GroupBy(t => t.Types.Count)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => g.OrderBy(_ => UnityEngine.Random.value))
                .ToList();

            foreach (var template in sortedTemplates)
            {
                bool canForm = true;
                // Aici verificăm pentru fiecare tip, ținând cont de aparițiile din template
                foreach (var group in template.Types.GroupBy(rt => rt))
                {
                    RoomType type = group.Key;
                    int neededCount = group.Count();
                    if (!pools.ContainsKey(type) || pools[type].Count < neededCount)
                    {
                        canForm = false;
                        break;
                    }
                }

                if (canForm)
                {
                    List<Room> group = new List<Room>();
                    // Extragem câte o cameră din pool pentru fiecare tip din template (în ordinea dată)
                    foreach (var rt in template.Types)
                    {
                        group.Add(pools[rt].Dequeue());
                    }
                    groups.Add(group);
                    formedGroup = true;
                }
            }
        }

        // 4. Pentru orice cameră rămasă în pool, formăm grupuri individuale.
        foreach (var kvp in pools)
        {
            while (kvp.Value.Count > 0)
            {
                groups.Add(new List<Room> { kvp.Value.Dequeue() });
            }
        }

        // Sortează grupurile astfel încât cele cu mai multe camere să aibă prioritate
        groups = groups.OrderByDescending(g => g.Count).ToList();

        // Debug (opțional): afișează grupurile generate
        Debug.Log("Grupuri generate din RoomGroupGenerator:");
        foreach (var group in groups)
        {
            string desc = "Grup (count " + group.Count + "): ";
            foreach (var room in group)
            {
                desc += room.GetRoomType() + " ";
            }
            Debug.Log(desc);
        }


        groups = groups
    .OrderByDescending(g => g.Count)
    .ToList();
        return groups;
    }


}
