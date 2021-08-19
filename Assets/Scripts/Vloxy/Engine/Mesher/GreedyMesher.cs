﻿using System.Linq;

using CodeBlaze.Vloxy.Engine.Data;

using UnityEngine;

namespace CodeBlaze.Vloxy.Engine.Mesher {

    public class GreedyMesher<B> : IMesher<B> where B : IBlock {

        protected readonly MeshData MeshData;

        protected MeshBuildJobData<B> JobData;

        private int _index;
        private Vector3Int _size;

        private readonly float[] AO_CURVE = { 0.75f, 0.825f, 0.9f, 1.0f };
        // private readonly float[] AO_CURVE = { 1.0f, 1.0f, 1.0f, 1.0f };

        public GreedyMesher() {
            MeshData = new MeshData();
            _size = VoxelProvider<B>.Current.Settings.Chunk.ChunkSize;
        }

        protected virtual void CreateQuad(Mask mask, Vector3Int normal) { }

        protected virtual bool CompareBlock(B block1, B block2) => block1.Equals(block2);
        
        protected readonly struct Mask {

            public readonly B Block;
            
            internal readonly sbyte Normal;
            internal readonly float[] AO;

            public Mask(B block, sbyte normal, float[] ao) {
                Block = block;
                Normal = normal;
                AO = ao;
            }

        }
        
        public MeshData GenerateMesh(MeshBuildJobData<B> data) {
            JobData = data;
            
            // Sweep over each axis (X, Y and Z)
            for (int direction = 0; direction < 3; direction++) {
                // 2 Perpendicular axis
                int axis1 = (direction + 1) % 3;
                int axis2 = (direction + 2) % 3;

                int mainAxisLimit = _size[direction];
                int axis1Limit = _size[axis1];
                int axis2Limit = _size[axis2];
                
                var deltaAxis1 = Vector3Int.zero;
                var deltaAxis2 = Vector3Int.zero;

                var chunkItr = Vector3Int.zero;
                var directionMask = Vector3Int.zero;
                directionMask[direction] = 1;

                var normalMask = new Mask[axis1Limit * axis2Limit];

                // Check each slice of the chunk one at a time
                for (chunkItr[direction] = -1; chunkItr[direction] < mainAxisLimit;) {
                    var n = 0;

                    // Compute the mask
                    for (chunkItr[axis2] = 0; chunkItr[axis2] < axis2Limit; ++chunkItr[axis2]) {
                        for (chunkItr[axis1] = 0; chunkItr[axis1] < axis1Limit; ++chunkItr[axis1]) {
                            var currentBlock = JobData.GetBlock(chunkItr);
                            var compareBlock = JobData.GetBlock(chunkItr + directionMask);

                            var currentBlockOpaque = currentBlock.IsOpaque();
                            var compareBlockOpaque = compareBlock.IsOpaque(); 

                            if (currentBlockOpaque == compareBlockOpaque) {
                                normalMask[n++] = default;
                            } else if (currentBlockOpaque) {
                                normalMask[n++] = new Mask(currentBlock, 1, ComputeAOMask(chunkItr + directionMask, axis1, axis2));
                            } else {
                                normalMask[n++] = new Mask(compareBlock, -1, ComputeAOMask(chunkItr, axis1, axis2));
                            }
                        }
                    }

                    ++chunkItr[direction];
                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,      
                    // by looping over each block in this slice of the chunk
                    for (int j = 0; j < axis2Limit; j++) {
                        for (int i = 0; i < axis1Limit;) {
                            if (normalMask[n].Normal != 0) {
                                // Current Stuff
                                var currentMask = normalMask[n];
                                chunkItr[axis1] = i;
                                chunkItr[axis2] = j;

                                // Compute the width of this quad and store it in w                        
                                // This is done by searching along the current axis until mask[n + w] is false
                                int width;

                                for (width = 1; i + width < axis1Limit && CompareMask(normalMask[n + width] , currentMask); width++) { }

                                // Compute the height of this quad and store it in h                        
                                // This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                // For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                // greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

                                int height;
                                bool done = false;
                                
                                for (height = 1; j + height < axis2Limit; height++) {
                                    // Check each block next to this quad
                                    for (int k = 0; k < width; ++k) {
                                        if (CompareMask(normalMask[n + k + height * axis1Limit] , currentMask)) continue;

                                        done = true;
                                        break; // If there's a hole in the mask, exit
                                    }

                                    if (done) break;
                                }
                                
                                deltaAxis1[axis1] = width;
                                deltaAxis2[axis2] = height;

                                // create quad
                                CreateQuad(
                                    currentMask, directionMask,
                                    chunkItr,
                                    chunkItr + deltaAxis1,
                                    chunkItr + deltaAxis2,
                                    chunkItr + deltaAxis1 + deltaAxis2
                                );

                                deltaAxis1 = Vector3Int.zero;
                                deltaAxis2 = Vector3Int.zero;
                                
                                // Clear this part of the mask, so we don't add duplicate faces
                                for (int l = 0; l < height; ++l)
                                    for (int k = 0; k < width; ++k)
                                        normalMask[n + k + l * axis1Limit] = default;

                                i += width;
                                n += width;
                            } else {
                                i++;
                                n++;
                            }
                        }
                    }
                }
            }

            return MeshData;
        }

        public void Clear() {
            JobData = null;
            MeshData.Clear();
            _index = 0;
        }

        private void CreateQuad(Mask mask, Vector3Int directionMask, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
            var normal = directionMask * mask.Normal;
            
            MeshData.Vertices.Add(v1);                              // 0 Bottom Left
            MeshData.Vertices.Add(v2);                              // 1 Top Left
            MeshData.Vertices.Add(v3);                              // 2 Bottom Right
            MeshData.Vertices.Add(v4);                              // 3 Top Right

            if (mask.AO[0] + mask.AO[3] > mask.AO[1] + mask.AO[2]) {    // + -
                MeshData.Triangles.Add(_index);                         // 0 0
                MeshData.Triangles.Add(_index + 2 - mask.Normal);   // 1 3
                MeshData.Triangles.Add(_index + 2 + mask.Normal);   // 3 1
                MeshData.Triangles.Add(_index + 3);                 // 3 3
                MeshData.Triangles.Add(_index + 1 + mask.Normal);   // 2 0
                MeshData.Triangles.Add(_index + 1 - mask.Normal);   // 0 2
            } else {                                                    // + -
                MeshData.Triangles.Add(_index + 1);                 // 1 1
                MeshData.Triangles.Add(_index + 1 + mask.Normal);   // 2 0
                MeshData.Triangles.Add(_index + 1 - mask.Normal);   // 0 2
                MeshData.Triangles.Add(_index + 2);                 // 2 2
                MeshData.Triangles.Add(_index + 2 - mask.Normal);   // 1 3
                MeshData.Triangles.Add(_index + 2 + mask.Normal);   // 3 1
            }

            // Required for circular AO
            MeshData.UV1.Add(Vector2.zero);
            MeshData.UV1.Add(Vector2.up);
            MeshData.UV1.Add(Vector2.right);
            MeshData.UV1.Add(Vector2.one);

            // Each vertex needs to know all the AO values of the face for bilinear interpolation
            MeshData.UV2.Add(new Vector4(mask.AO[0], mask.AO[1],mask.AO[2],mask.AO[3]));
            MeshData.UV2.Add(new Vector4(mask.AO[0], mask.AO[1],mask.AO[2],mask.AO[3]));
            MeshData.UV2.Add(new Vector4(mask.AO[0], mask.AO[1],mask.AO[2],mask.AO[3]));
            MeshData.UV2.Add(new Vector4(mask.AO[0], mask.AO[1],mask.AO[2],mask.AO[3]));
            
            MeshData.Normals.Add(normal);
            MeshData.Normals.Add(normal);
            MeshData.Normals.Add(normal);
            MeshData.Normals.Add(normal);

            _index += 4;
            
            CreateQuad(mask, normal);
        }

        private bool CompareMask(Mask m1, Mask m2) => CompareBlock(m1.Block, m2.Block) && m1.Normal == m2.Normal && Enumerable.SequenceEqual(m1.AO, m2.AO) ;

        private float[] ComputeAOMask(Vector3Int coord, int axis1, int axis2) {
            var L = coord;
            var R = coord;
            var B = coord;
            var T = coord;

            var LBC = coord;
            var RBC = coord;
            var LTC = coord;
            var RTC = coord;
            
            L[axis2] -= 1;
            R[axis2] += 1;
            B[axis1] -= 1;
            T[axis1] += 1;

            LBC[axis1] -= 1; LBC[axis2] -= 1;
            RBC[axis1] -= 1; RBC[axis2] += 1;
            LTC[axis1] += 1; LTC[axis2] -= 1;
            RTC[axis1] += 1; RTC[axis2] += 1;

         
            var LO = JobData.GetBlock(L).IsOpaque() ? 1 : 0;
            var RO = JobData.GetBlock(R).IsOpaque() ? 1 : 0;
            var BO = JobData.GetBlock(B).IsOpaque() ? 1 : 0;
            var TO = JobData.GetBlock(T).IsOpaque() ? 1 : 0;

            var LBCO = JobData.GetBlock(LBC).IsOpaque() ? 1 : 0;
            var RBCO = JobData.GetBlock(RBC).IsOpaque() ? 1 : 0;
            var LTCO = JobData.GetBlock(LTC).IsOpaque() ? 1 : 0;
            var RTCO = JobData.GetBlock(RTC).IsOpaque() ? 1 : 0;

            return new [] {
                AO_CURVE[ComputeAO(LO, BO, LBCO)],
                AO_CURVE[ComputeAO(LO, TO, LTCO)],
                AO_CURVE[ComputeAO(RO, BO, RBCO)],
                AO_CURVE[ComputeAO(RO, TO, RTCO)]
            }; 

        }

        private int ComputeAO(int s1, int s2, int c) {
            if (s1 == 1 && s2 == 1) {
                return 0;
            }

            return 3 - (s1 + s2 + c);
        }

    }

}