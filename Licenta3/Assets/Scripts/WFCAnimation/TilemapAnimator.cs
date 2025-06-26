using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapAnimator : MonoBehaviour
{
    [Header("Referințe")]
    public Tilemap tilemap;       // trage aici componenta Tilemap din scenă
    public TileBase[] tiles;      // array cu toate TileBase-urile, indexate după ID-ul pattern-ului

    [Header("Setări animație")]
    public float dropHeight = 5f;     // înălțimea de unde „cade” tile-ul
    public float dropDuration = 0.3f; // cât durează căderea (în secunde)
    public float delayBetween = 0.05f;// mică pauză între un tile și următorul

    /// <summary>
    /// Golește Tilemap-ul și pornește animația pentru întreaga listă de colapsări.
    /// </summary>
    void Awake()
    {
        // Încarcă **toate** TileBase-urile din Resources/Tiles
        tiles = Resources.LoadAll<TileBase>("Tiles");
        if (tiles == null || tiles.Length == 0)
            Debug.LogError("Niciun tile găsit în Resources/Tiles!");
    }

    public IEnumerator AnimateCollapseOrder(List<(Vector2Int pos, int pattern)> collapseOrder)
    {
        // 1. Curăță orice tile existent
        tilemap.ClearAllTiles();

        // 2. Pentru fiecare pas din colaps:
        foreach (var (pos2D, patt) in collapseOrder)
        {
            // 2.a. Animația de „drop” + punerea tile-ului
            yield return StartCoroutine(DropThenPlaceTile(pos2D, patt));
            // 2.b. Așteaptă puțin înainte de următorul tile
            yield return new WaitForSeconds(delayBetween);
        }
    }

    /// <summary>
    /// Creează un GameObject temporar cu sprite-ul corespunzător,
    /// îl lasă să cadă și abia apoi pune tile-ul în Tilemap.
    /// </summary>
    private IEnumerator DropThenPlaceTile(Vector2Int pos2D, int patternId)
    {
        // a) Creează GO temporar cu SpriteRenderer
        GameObject go = new GameObject("TileDrop");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = (tiles[patternId] as Tile)?.sprite;
        if (sr.sprite == null)
            Debug.LogError($"TileBase la indexul {patternId} nu conține un sprite!");

        // b) Calculează poziția finală (în world space) a celulei
        Vector3Int cell = new Vector3Int(pos2D.x, pos2D.y, 0);
        Vector3 targetPos = tilemap.CellToWorld(cell) + tilemap.tileAnchor;

        // c) Plasează GO-ul temporar sus la dropHeight
        go.transform.position = targetPos + Vector3.up * dropHeight;

        // d) Interpolează căderea
        float elapsed = 0f;
        Vector3 startPos = go.transform.position;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            go.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        go.transform.position = targetPos;

        // e) După animație, pune tile-ul în Tilemap
        tilemap.SetTile(cell, tiles[patternId]);

        // f) Distruge obiectul temporar
        Destroy(go);
    }
}
