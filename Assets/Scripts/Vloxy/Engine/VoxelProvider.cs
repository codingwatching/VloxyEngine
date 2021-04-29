﻿using System;
using System.Collections.Generic;

using CBSL.Core.Provider;

using CodeBlaze.Vloxy.Engine.Components;
using CodeBlaze.Vloxy.Engine.Data;
using CodeBlaze.Vloxy.Engine.Mesher;
using CodeBlaze.Vloxy.Engine.Schedular;
using CodeBlaze.Vloxy.Engine.Noise.Profile;
using CodeBlaze.Vloxy.Engine.Settings;

using UnityEngine;

namespace CodeBlaze.Vloxy.Engine {

    public class VoxelProvider<B> : Provider<VoxelProvider<B>> where B : IBlock {

        public VoxelSettings Settings { get; set; }

        public virtual ChunkDataPipeline<B> ChunkCreationPipeLine { get; } =
            new ChunkDataPipeline<B>(new List<Func<IChunkData<B>, IChunkData<B>>> {
                ChunkDataPipeline<B>.Functions.EmptyChunkRemover,
                ChunkDataPipeline<B>.Functions.ChunkDataCompressor
            });

        public virtual Chunk<B> CreateChunk(Vector3Int position) => new Chunk<B>(position);

        public virtual ChunkStore<B> ChunkStore(INoiseProfile<B> noiseProfile) => new ChunkStore<B>(noiseProfile);

        public virtual IChunkData<B> CreateChunkData(B[] blocks) => new CompressibleChunkData<B>(blocks);

        public virtual INoiseProfile<B> NoiseProfile() => null;

        public virtual ChunkBehaviourPool<B> ChunkPool(Transform transform) => new ChunkBehaviourPool<B>(transform);

        public virtual IMesher<B> MeshBuilder() => new GreedyMesher<B>();
        
        public virtual MeshBuildSchedular<B> MeshBuildCoordinator(ChunkBehaviourPool<B> chunkBehaviourPool) => new UniTaskMeshBuildSchedular<B>(chunkBehaviourPool);

    }

}