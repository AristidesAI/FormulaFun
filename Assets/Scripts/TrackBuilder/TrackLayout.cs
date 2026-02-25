using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FormulaFun.TrackBuilder
{
    [Serializable]
    public class TrackLayout
    {
        [JsonProperty("formatVersion")] public int FormatVersion;
        [JsonProperty("trackName")]     public string TrackName;
        [JsonProperty("author")]        public string Author;
        [JsonProperty("createdAt")]     public string CreatedAt;
        [JsonProperty("gridSize")]      public int GridSize;
        [JsonProperty("pieces")]        public List<TrackPiece> Pieces;
        [JsonProperty("metadata")]      public TrackMetadata Metadata;
    }

    [Serializable]
    public class TrackPiece
    {
        [JsonProperty("id")]         public int Id;
        [JsonProperty("modelId")]    public string ModelId;
        [JsonProperty("category")]   public string Category;
        [JsonProperty("gridX")]      public int GridX;
        [JsonProperty("gridZ")]      public int GridZ;
        [JsonProperty("rotationY")]  public int RotationY;
        [JsonProperty("gridWidth")]  public int GridWidth;
        [JsonProperty("gridDepth")]  public int GridDepth;
    }

    [Serializable]
    public class TrackMetadata
    {
        [JsonProperty("totalPieces")]   public int TotalPieces;
        [JsonProperty("hasStartLine")]  public bool HasStartLine;
        [JsonProperty("hasEndOrLoop")]  public bool HasEndOrLoop;
    }
}
