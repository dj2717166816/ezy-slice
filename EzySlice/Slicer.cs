using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {

    /**
     * Contains methods for slicing GameObjects
     */
    public sealed class Slicer {

        /**
         * An internal class for storing internal submesh values
         */
        //包含两个部分的三角形列表
        internal class SlicedSubmesh {
            public readonly List<Triangle> upperHull = new List<Triangle>();
            public readonly List<Triangle> lowerHull = new List<Triangle>();

            /**
             * Check if the submesh has had any UV's added.
             * NOTE -> This should be supported properly
             */
            public bool hasUV {
                get {
                    // what is this abomination??
                    return upperHull.Count > 0 ? upperHull[0].hasUV : lowerHull.Count > 0 && lowerHull[0].hasUV;
                }
            }

            /**
             * Check if the submesh has had any Normals added.
             * NOTE -> This should be supported properly
             */
            public bool hasNormal {
                get {
                    // what is this abomination??
                    return upperHull.Count > 0 ? upperHull[0].hasNormal : lowerHull.Count > 0 && lowerHull[0].hasNormal;
                }
            }

            /**
             * Check if the submesh has had any Tangents added.
             * NOTE -> This should be supported properly
             */
            public bool hasTangent {
                get {
                    // what is this abomination??
                    return upperHull.Count > 0 ? upperHull[0].hasTangent : lowerHull.Count > 0 && lowerHull[0].hasTangent;
                }
            }

            /**
             * Check if proper slicing has occured for this submesh. Slice occured if there
             * are triangles in both the upper and lower hulls
             */
            public bool isValid {
                get {
                    return upperHull.Count > 0 && lowerHull.Count > 0;
                }
            }
        }

        /**
         * Helper function to accept a gameobject which will transform the plane
         * approprietly before the slice occurs
         * See -> Slice(Mesh, Plane) for more info
         */
        //输入待切割物体，切割平面，材质范围大小，截面材质
        //主要是检查工作
        public static SlicedHull Slice(GameObject obj, Plane pl, TextureRegion crossRegion, Material crossMaterial) {
            
            // cannot continue without a proper filter
            if (!obj.TryGetComponent<MeshFilter>(out var filter)) {
                Debug.LogWarning("EzySlice::Slice -> Provided GameObject must have a MeshFilter Component.");

                return null;
            }

            
            // cannot continue without a proper renderer
            if (!obj.TryGetComponent<MeshRenderer>(out var renderer)) {
                Debug.LogWarning("EzySlice::Slice -> Provided GameObject must have a MeshRenderer Component.");

                return null;
            }

            Material[] materials = renderer.sharedMaterials;

            Mesh mesh = filter.sharedMesh;

            // cannot slice a mesh that doesn't exist
            if (mesh == null) {
                Debug.LogWarning("EzySlice::Slice -> Provided GameObject must have a Mesh that is not NULL.");

                return null;
            }

            int submeshCount = mesh.subMeshCount;

            // to make things straightforward, exit without slicing if the materials and mesh
            // array don't match. This shouldn't happen anyway
            if (materials.Length != submeshCount) {
                Debug.LogWarning("EzySlice::Slice -> Provided Material array must match the length of submeshes.");

                return null;
            }

            // we need to find the index of the material for the cross section.
            // default to the end of the array
            int crossIndex = materials.Length;

            // for cases where the sliced material is null, we will append the cross section to the end
            // of the submesh array, this is because the application may want to set/change the material
            // after slicing has occured, so we don't assume anything
            if (crossMaterial != null) {
                for (int i = 0; i < crossIndex; i++) {
                    if (materials[i] == crossMaterial) {
                        crossIndex = i;
                        break;
                    }
                }
            }

            return Slice(mesh, pl, crossRegion, crossIndex);
        }

        /**
         * Slice the gameobject mesh (if any) using the Plane, which will generate
         * a maximum of 2 other Meshes.
         * This function will recalculate new UV coordinates to ensure textures are applied
         * properly.
         * Returns null if no intersection has been found or the GameObject does not contain
         * a valid mesh to cut.
         */
        public static SlicedHull Slice(Mesh sharedMesh, Plane pl, TextureRegion region, int crossIndex) {
            if (sharedMesh == null) {
                return null;
            }

            Vector3[] verts = sharedMesh.vertices;
            Vector2[] uv = sharedMesh.uv;
            Vector3[] norm = sharedMesh.normals;
            Vector4[] tan = sharedMesh.tangents;

            int submeshCount = sharedMesh.subMeshCount;

            // each submesh will be sliced and placed in its own array structure
            //每个子网格都会有一个slice存它被切割后的两部分的三角形
            SlicedSubmesh[] slices = new SlicedSubmesh[submeshCount];
            // the cross section hull is common across all submeshes
            List<Vector3> crossHull = new List<Vector3>();

            // we reuse this object for all intersection tests
            IntersectionResult result = new IntersectionResult();

            // see if we would like to split the mesh using uv, normals and tangents
            bool genUV = verts.Length == uv.Length;
            bool genNorm = verts.Length == norm.Length;
            bool genTan = verts.Length == tan.Length;

            // iterate over all the submeshes individually. vertices and indices
            // are all shared within the submesh
            for (int submesh = 0; submesh < submeshCount; submesh++) {
                int[] indices = sharedMesh.GetTriangles(submesh);//三角形的三个坐标们
                int indicesCount = indices.Length;

                SlicedSubmesh mesh = new SlicedSubmesh();

                // loop through all the mesh vertices, generating upper and lower hulls
                // and all intersection points
                for (int index = 0; index < indicesCount; index += 3) {
                    int i0 = indices[index + 0];
                    int i1 = indices[index + 1];
                    int i2 = indices[index + 2];

                    Triangle newTri = new Triangle(verts[i0], verts[i1], verts[i2]);

                    // generate UV if available
                    if (genUV) {
                        newTri.SetUV(uv[i0], uv[i1], uv[i2]);
                    }

                    // generate normals if available
                    if (genNorm) {
                        newTri.SetNormal(norm[i0], norm[i1], norm[i2]);
                    }

                    // generate tangents if available
                    if (genTan) {
                        newTri.SetTangent(tan[i0], tan[i1], tan[i2]);
                    }

                    // slice this particular triangle with the provided
                    // plane
                    if (newTri.Split(pl, result)) {//切割三角形并判断结果是否合法，调用时清空了之前的result内容
                        int upperHullCount = result.upperHullCount;
                        int lowerHullCount = result.lowerHullCount;
                        int interHullCount = result.intersectionPointCount;

                        //以下为添加三角形
                        for (int i = 0; i < upperHullCount; i++) {
                            mesh.upperHull.Add(result.upperHull[i]);
                        }

                        for (int i = 0; i < lowerHullCount; i++) {
                            mesh.lowerHull.Add(result.lowerHull[i]);
                        }

                        for (int i = 0; i < interHullCount; i++) {
                            crossHull.Add(result.intersectionPoints[i]);
                        }
                    } 
                    else {//非法时？
                        SideOfPlane sa = pl.SideOf(verts[i0]);
                        SideOfPlane sb = pl.SideOf(verts[i1]);
                        SideOfPlane sc = pl.SideOf(verts[i2]);

                        SideOfPlane side = SideOfPlane.ON;
                        if (sa != SideOfPlane.ON)
                        {
                            side = sa;
                        }
                        
                        if (sb != SideOfPlane.ON)
                        {
                            Debug.Assert(side == SideOfPlane.ON || side == sb);
                            side = sb;
                        }
                        
                        if (sc != SideOfPlane.ON)
                        {
                            Debug.Assert(side == SideOfPlane.ON || side == sc);
                            side = sc;
                        }

                        if (side == SideOfPlane.UP || side == SideOfPlane.ON) {
                            mesh.upperHull.Add(newTri);
                        } 
                        else {
                            mesh.lowerHull.Add(newTri);
                        }
                    }
                }

                // register into the index
                slices[submesh] = mesh;
            }

            // check if slicing actually occured
            for (int i = 0; i < slices.Length; i++) {
                // check if at least one of the submeshes was sliced. If so, stop checking
                // because we need to go through the generation step
                if (slices[i] != null && slices[i].isValid) {
                    //传入上下部分的三角形列表（slice中），截面的交点，平面法线，材质范围，切面材质编号
                    return CreateFrom(slices, CreateFrom(crossHull, pl.normal, region)/*用算法计算截面三角形*/, crossIndex);//返回切割后的上下网格在slicehull里
                }
            }

            // no slicing occured, just return null to signify
            return null;
        }

        /**
         * Generates a single SlicedHull from a set of cut submeshes 
         */
        //根据上下部分的三角形列表，截面三角形列表，材质编号，返回上下部分的网格
        private static SlicedHull CreateFrom(SlicedSubmesh[] meshes, List<Triangle> cross, int crossSectionIndex) {
            int submeshCount = meshes.Length;

            int upperHullCount = 0;
            int lowerHullCount = 0;

            // get the total amount of upper, lower and intersection counts
            for (int submesh = 0; submesh < submeshCount; submesh++) {
                upperHullCount += meshes[submesh].upperHull.Count;//所有子网格的上部分三角形数
                lowerHullCount += meshes[submesh].lowerHull.Count;//所有子网格的下部分三角形数
            }

            Mesh upperHull = CreateUpperHull(meshes, upperHullCount, cross, crossSectionIndex);
            Mesh lowerHull = CreateLowerHull(meshes, lowerHullCount, cross, crossSectionIndex);

            return new SlicedHull(upperHull, lowerHull);
        }

        private static Mesh CreateUpperHull(SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossSectionIndex) {
            return CreateHull(mesh, total, crossSection, crossSectionIndex, true);
        }

        private static Mesh CreateLowerHull(SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossSectionIndex) {
            return CreateHull(mesh, total, crossSection, crossSectionIndex, false);
        }

        /**
         * Generate a single Mesh HULL of either the UPPER or LOWER hulls. 
         */
        //根据子网格（只有三角形），此部分总三角形数，截面三角形，截面材质，生成真正的mesh
        private static Mesh CreateHull(SlicedSubmesh[] meshes, int total, List<Triangle> crossSection, int crossIndex, bool isUpper) {
            if (total <= 0) {
                return null;
            }

            int submeshCount = meshes.Length;//子网格数
            int crossCount = crossSection != null ? crossSection.Count : 0;//截面三角形面积

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;//选择32位索引，支持超过40亿个顶点
            
            int arrayLen = (total + crossCount) * 3;//所有的点数

            bool hasUV = meshes[0].hasUV;
            bool hasNormal = meshes[0].hasNormal;
            bool hasTangent = meshes[0].hasTangent;

            // vertices and uv's are common for all submeshes
            Vector3[] newVertices = new Vector3[arrayLen];
            Vector2[] newUvs = hasUV ? new Vector2[arrayLen] : null;
            Vector3[] newNormals = hasNormal ? new Vector3[arrayLen] : null;
            Vector4[] newTangents = hasTangent ? new Vector4[arrayLen] : null;

            // each index refers to our submesh triangles
            List<int[]> triangles = new List<int[]>(submeshCount);//三角形对应的点的索引

            int vIndex = 0;

            // first we generate all our vertices, uv's and triangles
            for (int submesh = 0; submesh < submeshCount; submesh++) {
                // pick the hull we will be playing around with
                List<Triangle> hull = isUpper ? meshes[submesh].upperHull : meshes[submesh].lowerHull;
                int hullCount = hull.Count;//采用的部分的三角形数

                int[] indices = new int[hullCount * 3];//索引集

                // fill our mesh arrays
                for (int i = 0, triIndex = 0; i < hullCount; i++, triIndex += 3) {
                    Triangle newTri = hull[i];
                    
                    //点在总点集中的索引
                    int i0 = vIndex + 0;
                    int i1 = vIndex + 1;
                    int i2 = vIndex + 2;

                    // add the vertices
                    //添加点
                    newVertices[i0] = newTri.positionA;
                    newVertices[i1] = newTri.positionB;
                    newVertices[i2] = newTri.positionC;

                    // add the UV coordinates if any
                    if (hasUV) {
                        newUvs[i0] = newTri.uvA;
                        newUvs[i1] = newTri.uvB;
                        newUvs[i2] = newTri.uvC;
                    }

                    // add the Normals if any
                    if (hasNormal) {
                        newNormals[i0] = newTri.normalA;
                        newNormals[i1] = newTri.normalB;
                        newNormals[i2] = newTri.normalC;
                    }

                    // add the Tangents if any
                    if (hasTangent) {
                        newTangents[i0] = newTri.tangentA;
                        newTangents[i1] = newTri.tangentB;
                        newTangents[i2] = newTri.tangentC;
                    }

                    // triangles are returned in clocwise order from the
                    // intersector, no need to sort these
                    //添加三角形对点索引的记录
                    indices[triIndex] = i0;
                    indices[triIndex + 1] = i1;
                    indices[triIndex + 2] = i2;

                    vIndex += 3;
                }

                // add triangles to the index for later generation
                triangles.Add(indices);
            }

            // generate the cross section required for this particular hull
            //同上部分，对截面的三角星和点进行处理
            if (crossSection != null && crossCount > 0) {
                int[] crossIndices = new int[crossCount * 3];//截面三角形点的索引

                for (int i = 0, triIndex = 0; i < crossCount; i++, triIndex += 3) {
                    Triangle newTri = crossSection[i];

                    int i0 = vIndex + 0;
                    int i1 = vIndex + 1;
                    int i2 = vIndex + 2;

                    // add the vertices
                    newVertices[i0] = newTri.positionA;
                    newVertices[i1] = newTri.positionB;
                    newVertices[i2] = newTri.positionC;

                    // add the UV coordinates if any
                    if (hasUV) {
                        newUvs[i0] = newTri.uvA;
                        newUvs[i1] = newTri.uvB;
                        newUvs[i2] = newTri.uvC;
                    }

                    // add the Normals if any
                    if (hasNormal) {
                        // invert the normals dependiong on upper or lower hull
                        if (isUpper) {
                            newNormals[i0] = -newTri.normalA;
                            newNormals[i1] = -newTri.normalB;
                            newNormals[i2] = -newTri.normalC;
                        } else {
                            newNormals[i0] = newTri.normalA;
                            newNormals[i1] = newTri.normalB;
                            newNormals[i2] = newTri.normalC;
                        }
                    }

                    // add the Tangents if any
                    if (hasTangent) {
                        newTangents[i0] = newTri.tangentA;
                        newTangents[i1] = newTri.tangentB;
                        newTangents[i2] = newTri.tangentC;
                    }

                    // add triangles in clockwise for upper
                    // and reversed for lower hulls, to ensure the mesh
                    // is facing the right direction
                    if (isUpper) {
                        crossIndices[triIndex] = i0;
                        crossIndices[triIndex + 1] = i1;
                        crossIndices[triIndex + 2] = i2;
                    } else {
                        crossIndices[triIndex] = i0;
                        crossIndices[triIndex + 1] = i2;
                        crossIndices[triIndex + 2] = i1;
                    }

                    vIndex += 3;
                }

                // add triangles to the index for later generation
                if (triangles.Count <= crossIndex) {
                    triangles.Add(crossIndices);
                } 
                else {
                    // otherwise, we need to merge the triangles for the provided subsection
                    //融合和截面相同材质的子网格
                    int[] prevTriangles = triangles[crossIndex];
                    int[] merged = new int[prevTriangles.Length + crossIndices.Length];

                    System.Array.Copy(prevTriangles, merged, prevTriangles.Length);
                    System.Array.Copy(crossIndices, 0, merged, prevTriangles.Length, crossIndices.Length);

                    // replace the previous array with the new merged array
                    triangles[crossIndex] = merged;
                }
            }

            int totalTriangles = triangles.Count;

            newMesh.subMeshCount = totalTriangles;
            // fill the mesh structure
            newMesh.vertices = newVertices;

            if (hasUV) {
                newMesh.uv = newUvs;
            }

            if (hasNormal) {
                newMesh.normals = newNormals;
            }

            if (hasTangent) {
                newMesh.tangents = newTangents;
            }

            // add the submeshes
            for (int i = 0; i < totalTriangles; i++) {
                newMesh.SetTriangles(triangles[i], i, false);
            }

            return newMesh;
        }

        /**
         * Generate Two Meshes (an upper and lower) cross section from a set of intersection
         * points and a plane normal. Intersection Points do not have to be in order.
         */
//此函数之后要改成调用新截面算法
        private static List<Triangle> CreateFrom(List<Vector3> intPoints, Vector3 planeNormal, TextureRegion region) {
            return Triangulator.MonotoneChain(intPoints, planeNormal, out List<Triangle> tris, region) ? tris : null;
        }
    }
}
