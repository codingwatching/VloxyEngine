﻿using System.Collections.Generic;

using CodeBlaze.Vloxy.Engine.Data;
using CodeBlaze.Vloxy.Engine.Meshing.Coordinator;
using CodeBlaze.Vloxy.Engine.Noise.Profile;
using CodeBlaze.Vloxy.Engine.Settings;

using UnityEngine;

namespace CodeBlaze.Vloxy.Engine.Components {

    public class ChunkStore<B> where B : IBlock {

        private const string TAG = "<color=orange>ChunkStore</color>";
        
        protected Dictionary<Vector3Int, Chunk<B>> Chunks;

        private INoiseProfile<B> _noiseProfile;
        private ChunkSettings _chunkSettings;

        public ChunkStore(INoiseProfile<B> noiseProfile, ChunkSettings chunkSettings) {
            _noiseProfile = noiseProfile;
            _chunkSettings = chunkSettings;
            Chunks = new Dictionary<Vector3Int, Chunk<B>>();
        }

        public bool ContainsChunk(Vector3Int coord) => Chunks.ContainsKey(coord);
        
        public void GenerateChunks() {
            for (int x = -_chunkSettings.ChunkPageSize; x < _chunkSettings.ChunkPageSize; x++) {
                for (int z = -_chunkSettings.ChunkPageSize; z < _chunkSettings.ChunkPageSize; z++) {
                    for (int y = -_chunkSettings.ChunkPageSize; y < _chunkSettings.ChunkPageSize; y++) {
                        var pos = new Vector3Int(x, y, z) * _chunkSettings.ChunkSize;
                        var chunk = VoxelProvider<B>.Current.CreateChunk(pos);
                        chunk.Data = _noiseProfile.GenerateChunkData(pos);
                        Chunks.Add(pos, chunk);
                    }
                }
            }
            
            Debug.unityLogger.Log(TAG,"Chunks Created : " + Chunks.Count);
        }
        
        public MeshBuildJobData<B> GetChunkJobData(Vector3Int position) {
            var px = position + Vector3Int.right * _chunkSettings.ChunkSize;
            var py = position + Vector3Int.up * _chunkSettings.ChunkSize;
            var pz = position + new Vector3Int(0, 0, 1) * _chunkSettings.ChunkSize;
            var nx = position + Vector3Int.left * _chunkSettings.ChunkSize;
            var ny = position + Vector3Int.down * _chunkSettings.ChunkSize;
            var nz = position + new Vector3Int(0, 0, -1) * _chunkSettings.ChunkSize;
            
            return new MeshBuildJobData<B> {
                Chunk = Chunks[position],
                ChunkPX = Chunks.ContainsKey(px) ? Chunks[px] : null,
                ChunkPY = Chunks.ContainsKey(py) ? Chunks[py] : null,
                ChunkPZ = Chunks.ContainsKey(pz) ? Chunks[pz] : null,
                ChunkNX = Chunks.ContainsKey(nx) ? Chunks[nx] : null,
                ChunkNY = Chunks.ContainsKey(ny) ? Chunks[ny] : null,
                ChunkNZ = Chunks.ContainsKey(nz) ? Chunks[nz] : null
            };
        }

        #region Neighbors
        public Chunk<B> GetNeighborPX(Chunk<B> chunk) {
            var px = chunk.Position + Vector3Int.right * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(px) ? Chunks[px] : null;
        }
        
        public Chunk<B> GetNeighborPY(Chunk<B> chunk) {
            var py = chunk.Position + Vector3Int.up * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(py) ? Chunks[py] : null;
        }
        
        public Chunk<B> GetNeighborPZ(Chunk<B> chunk) {
            var pz = chunk.Position + new Vector3Int(0, 0, 1) * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(pz) ? Chunks[pz] : null;
        }
        
        public Chunk<B> GetNeighborNX(Chunk<B> chunk) {
            var nx = chunk.Position + Vector3Int.left * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(nx) ? Chunks[nx] : null;
        }
        
        public Chunk<B> GetNeighborNY(Chunk<B> chunk) {
            var ny = chunk.Position + Vector3Int.down * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(ny) ? Chunks[ny] : null;
        }
        
        public Chunk<B> GetNeighborNZ(Chunk<B> chunk) {
            var nz = chunk.Position + new Vector3Int(0, 0, -1) * _chunkSettings.ChunkSize;

            return Chunks.ContainsKey(nz) ? Chunks[nz] : null;
        }
        
        #endregion

    }

}