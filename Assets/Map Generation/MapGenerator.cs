﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RegionType { Water, Land, Mountain }

[System.Serializable]
public struct TerrainType {
    public string name;

    public float noiseBaseline;
    public float noiseMax;

    public bool walkable;
    public RegionType regionType;

    public Color color;
    public bool plateau;
    public bool plateauAtBase;

    public override bool Equals(object obj) {
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return regionType.ToString();
    }

    public bool ValueIsMember(float value) {
        return value <= noiseMax && value >= noiseBaseline;
    }

    public static bool operator ==(TerrainType obj1, TerrainType obj2) {
        return obj1.regionType == obj2.regionType;
    }

    public static bool operator !=(TerrainType obj1, TerrainType obj2) {
        return !(obj1 == obj2);
    }
}

public class MapGenerator : MonoBehaviour {

    public enum DrawMode {NoiseMap, ColorMap, Mesh}
    public DrawMode drawMode;

    private int layoutMapWidth;
    private int layoutMapHeight;

    public NoiseData layoutMapNoiseData;
    public NoiseData groundFeaturesMapNoiseData;
    public NoiseData mountainFeaturesMapNoiseData;

    [Range(0, 2)]
    public float groundFeaturesImpactOnLayout;

    [Range(0, 2)]
    public float mountainFeaturesImpactOnLayout;

    private int featuresPerLayoutPerAxis;    

    public bool autoUpdate;

    public TerrainType[] regions;

    public TerrainType TerrainForRegion(RegionType regionType) {
        foreach (TerrainType type in regions) {
            if (type.regionType == regionType) {
                return type;
            }
        }

        return regions[0];
    }

    public GameObject mapObject;

    private void OnValidate() {
        if(layoutMapWidth < 1) {
            layoutMapWidth = 1;
        }

        if(layoutMapHeight < 1) {
            layoutMapHeight = 1;
        }
    }

    public Map GenerateMap() {

        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        this.layoutMapWidth = constants.layoutMapWidth;
        this.layoutMapHeight = constants.layoutMapHeight;
        this.featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        // First generate a small noise map, use for the general layout (eg. which area is a path, which is a rock, ...)
        // This is the noise map the grid and selectons will use
        float[,] layoutNoiseMap = NoiseGenerator.GenerateNoiseMap(layoutMapWidth, layoutMapHeight, layoutMapNoiseData);

        int noiseMapWidth = layoutMapWidth * featuresPerLayoutPerAxis;
        int noiseMapHeight = layoutMapHeight * featuresPerLayoutPerAxis;

        // Then generate a larger scale noise map, and overlay it on the small one
        float[,] groundFeaturesNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, groundFeaturesMapNoiseData);
        float[,] mountainFeaturesNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, mountainFeaturesMapNoiseData);

        TerrainType[,] terrainMap = PlateauMap(layoutNoiseMap);
        float[,] noiseMap = CreateMapWithFeatures(layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap, terrainMap);

        NormalizeMap(noiseMap);

        Map map = new Map(noiseMap, layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap,
            featuresPerLayoutPerAxis,
            MeshGenerator.GenerateTerrainMesh(noiseMap, featuresPerLayoutPerAxis),
            TextureGenerator.TextureFromColorMap(CreateColorMapWithTerrain(noiseMap, terrainMap), noiseMapWidth, noiseMapHeight),
            terrainMap
            );

        MapDebugDisplay debugDisplay = FindObjectOfType<MapDebugDisplay>();
        switch (drawMode) {
            case DrawMode.NoiseMap:
                if (debugDisplay != null) {
                    debugDisplay.DrawTexture(TextureGenerator.TextureFromNoiseMap(noiseMap));
                    debugDisplay.DrawTextures(TextureGenerator.TextureFromNoiseMap(layoutNoiseMap), TextureGenerator.TextureFromNoiseMap(groundFeaturesNoiseMap));
                }                
                break;
            case DrawMode.ColorMap:
                if(debugDisplay != null) {

                    debugDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(CreateColorMap(noiseMap), noiseMap.GetLength(0), noiseMap.GetLength(1)));
                    debugDisplay.DrawTextures(TextureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                        TextureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                        );
                }
                break;
            case DrawMode.Mesh:

                MapContainer mapContainer = Tag.Map.GetGameObject().GetComponent<MapContainer>();
                mapContainer.setMap(map);

                if(debugDisplay != null) {
                        debugDisplay.DrawMeshes(MeshGenerator.GenerateTerrainMesh(layoutNoiseMap, 1), TextureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                        MeshGenerator.GenerateTerrainMesh(groundFeaturesNoiseMap, 1), TextureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                        );
                }
                break;
        }
       
        return map;
    }

    public float[,] TerraformHeightMap(float[,] layoutNoiseMap, float[,] groundFeaturesNoiseMap, float[,] mountainFeaturesNoiseMap, TerrainType[,] terrainMap, float currentLayoutHeight, LayoutCoordinate coordinate) {
        // TODO More interesting interpolations to mimic mining

        layoutNoiseMap[coordinate.x, coordinate.y] = currentLayoutHeight;

        float[,] noiseMap = CreateMapWithFeatures(layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap, terrainMap);
        NormalizeMap(noiseMap);

        return noiseMap;
    }

    // PRIVATE

    private float[,] CreateMapWithFeatures(float[,] layoutMap, float[,] groundFeaturesMap, float[,] mountainFeaturesMap, TerrainType[,] terrainMap) {
        int featuresWidth = groundFeaturesMap.GetLength(0);
        int featuresHeight = groundFeaturesMap.GetLength(1);

        float[,] fullMap = new float[featuresWidth, featuresHeight];
        for(int y = 0; y < featuresWidth; y++) {
            for(int x = 0; x < featuresHeight; x++) {
                int sampleX = x / featuresPerLayoutPerAxis;
                int sampleY = y / featuresPerLayoutPerAxis;

                switch(terrainMap[sampleX, sampleY].regionType) {
                    case RegionType.Water:
                        fullMap[x, y] = layoutMap[sampleX, sampleY];
                        break;
                    case RegionType.Land:
                        fullMap[x, y] = (layoutMap[sampleX, sampleY]) + ((groundFeaturesMap[x, y] * groundFeaturesImpactOnLayout) - 0.5f);
                        break;
                    case RegionType.Mountain:
                        fullMap[x, y] = layoutMap[sampleX, sampleY] + (mountainFeaturesMap[x, y] * mountainFeaturesImpactOnLayout);
                        break;
                }
            }
        }

        return fullMap;
    }

    // TEXTURES

    private Color[] CreateColorMap(float[,] noiseMap) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);



        Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
        for(int y = 0; y < noiseMapHeight; y++) {
            for(int x = 0; x < noiseMapWidth; x++) {

                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++) {
                    if(regions[i].ValueIsMember(currentHeight)) {
                        colorMap[y * noiseMapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return colorMap;
    }

    private Color[] CreateColorMapWithTerrain(float[,] noiseMap, TerrainType[,] terrainTypeMap) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        int terrainMapWidth = terrainTypeMap.GetLength(0);
        int terrainMapHeight = terrainTypeMap.GetLength(1);

        //PlayerBehaviour beh = GetComponent<PlayerBehaviour>();

        Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
        for(int y = 0; y < noiseMapHeight; y++) {
            for(int x = 0; x < noiseMapWidth; x++) {

                MapCoordinate coordinate = new MapCoordinate(x, y);

                if((x + 1) % featuresPerLayoutPerAxis == 0 || (y + 1) % featuresPerLayoutPerAxis == 0) {
                    colorMap[y * noiseMapWidth + x] = Color.white;
                    continue;
                }

                int sampleX = x / (noiseMapWidth / terrainMapWidth);
                int sampleY = y / (noiseMapHeight / terrainMapHeight);

                colorMap[y * noiseMapWidth + x] = terrainTypeMap[sampleX, sampleY].color;

                //PlayerBehaviour beh = Tag.Narrator.GetGameObject().GetComponent<PlayerBehaviour>();
                //if(coordinate == beh.selectedLayoutTile) {
                //    colorMap[y * noiseMapWidth + x] = colorMap[y * noiseMapWidth + x] + Color.cyan;
                //}
            }
        }

        return colorMap;
    }

    // Returns a Map of terrainTypes
    private TerrainType[,] PlateauMap(float[,] map) {
        int mapWidth = map.GetLength(0);
        int mapHeight = map.GetLength(1);

        TerrainType[,] terrainMap = new TerrainType[map.GetLength(0), map.GetLength(1)];

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                for(int i = 0; i < regions.Length; i++) {
                    if(regions[i].plateau && regions[i].ValueIsMember(map[x, y])) {

                        map[x, y] = (regions[i].plateauAtBase ? regions[i].noiseBaseline + 0.001f : regions[i].noiseMax - 0.001f);
                        terrainMap[x, y] = regions[i];
                        break;
                    }
                }
            }
        }

        return terrainMap;
    }

    private void NormalizeMap(float[,] noiseMap) {
        int mapWidth = noiseMap.GetLength(0);
        int mapHeight = noiseMap.GetLength(1);

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        for(int y = 0; y < mapWidth; y++) {
            for(int x = 0; x < mapHeight; x++) {
                int sampleX = x / featuresPerLayoutPerAxis;
                int sampleY = y / featuresPerLayoutPerAxis;

                float noiseHeight = noiseMap[x, y];

                if(noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }

                if(noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }
            }
        }

        // Normalize noise map
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
    }
}
