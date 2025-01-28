using UnityEngine;
using System.Collections;

public static class Noise
{
    public enum NormalizeMode { Local, Global };

    public static void CalculateGlobalMinMax(int worldWidth, int worldHeight, int seed, float scale, int octaves, float persistance, float lacunarity, out float globalMinNoiseHeight, out float globalMaxNoiseHeight)
    {
        globalMinNoiseHeight = float.MaxValue;
        globalMaxNoiseHeight = float.MinValue;

        float[,] tempNoiseMap = GenerateNoiseMap(worldWidth, worldHeight, seed, scale, octaves, persistance, lacunarity, Vector2.zero, NormalizeMode.Local);

        for (int y = 0; y < worldHeight; y++)
        {
            for (int x = 0; x < worldWidth; x++)
            {
                if (tempNoiseMap[x, y] < globalMinNoiseHeight)
                {
                    globalMinNoiseHeight = tempNoiseMap[x, y];
                }
                if (tempNoiseMap[x, y] > globalMaxNoiseHeight)
                {
                    globalMaxNoiseHeight = tempNoiseMap[x, y];
                }
            }
        }
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        // Define the border size
        int borderSize = 2;
        int borderedMapWidth = mapWidth + borderSize * 2;
        int borderedMapHeight = mapHeight + borderSize * 2;

        float[,] noiseMap = new float[borderedMapWidth, borderedMapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = borderedMapWidth / 2f;
        float halfHeight = borderedMapHeight / 2f;

        for (int y = 0; y < borderedMapHeight; y++)
        {
            for (int x = 0; x < borderedMapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normalize the noise map
        for (int y = 0; y < borderedMapHeight; y++)
        {
            for (int x = 0; x < borderedMapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        // Extract the central part of the noise map that corresponds to the actual chunk
        float[,] finalNoiseMap = new float[mapWidth, mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                finalNoiseMap[x, y] = noiseMap[x + borderSize, y + borderSize];
            }
        }

        return finalNoiseMap;
    }
}