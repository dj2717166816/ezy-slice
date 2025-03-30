using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EzySlice;
using UnityEditor;
using System;
using Ezyslice;

namespace EzySlice {

    public sealed class Slicer {

        /**
         * An internal class for storing internal submesh values
         */
        //包含两个部分的三角形列表
        internal class SlicedSubmesh {
            public List<Triangle> upperHull = new List<Triangle>();
            public List<Triangle> lowerHull = new List<Triangle>();

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

        public static bool is_equal(Vector3D a, Vector3D b)
        {
            double Toler = 1e-4f;

            return Math.Abs(a.x - b.x) < Toler &&
                   Math.Abs(a.y - b.y) < Toler &&
                   Math.Abs(a.z - b.z) < Toler;
        } 

        public class LineComparer : IEqualityComparer<Line>
        {
            private const double Tolerance = 1e-4f; // 允许的误差

            public bool Equals(Line a, Line b)
            {
                return (Equals(a.positionA, b.positionA) && Equals(a.positionB, b.positionB)) ||
                       (Equals(a.positionA, b.positionB) && Equals(a.positionB, b.positionA));
            }

            private bool Equals(Vector3D a, Vector3D b)
            {
                return Math.Abs(a.x - b.x) < Tolerance &&
                       Math.Abs(a.y - b.y) < Tolerance &&
                       Math.Abs(a.z - b.z) < Tolerance;
            }

            public int GetHashCode(Line obj)
            {
                return GetHashCodeUnordered(obj.positionB, obj.positionB);
            }

            private int GetHashCodeUnordered(Vector3D a, Vector3D b)
            {
                unchecked
                {
                    int hashA = GetHashCode(a);
                    int hashB = GetHashCode(b);
                    return hashA < hashB ? hashA + hashB * 31 : hashB + hashA * 31;
                }
            }

            private int GetHashCode(Vector3D obj)
            {
                unchecked
                {
                    int xHash = (int)(obj.x * 10000) * 73856093;
                    int yHash = (int)(obj.y * 10000) * 19349663;
                    int zHash = (int)(obj.z * 10000) * 83492791;
                    return xHash ^ yHash ^ zHash;
                }
            }
        }
        public static SlicedHull Slice(Mesh sharedMesh, Plane pl, TextureRegion region, int crossIndex) {
            if (sharedMesh == null) {
                return null;
            }


            Vector3[] vertexs = sharedMesh.vertices;
            Vector2[] uv = sharedMesh.uv;
            Vector3[] normS = sharedMesh.normals;
            Vector4[] tan = sharedMesh.tangents;
            Vector3D[] verts = new Vector3D[vertexs.Length];
            Vector3D[] norm = new Vector3D[normS.Length];

            for (int i = 0; i < vertexs.Length; i++)
            {
                verts[i] = new Vector3D(vertexs[i]);
            }

            for (int i = 0; i < normS.Length; i++)
            {
                norm[i] = new Vector3D(normS[i]);
            }

            int submeshCount = sharedMesh.subMeshCount;

            //每个子网格都会有一个slice存它被切割后的两部分的三角形
            SlicedSubmesh[] slices = new SlicedSubmesh[submeshCount];

            IntersectionResult result = new IntersectionResult();
            List<Triangle> cross = new List<Triangle>();//截面三角形

            bool genUV = verts.Length == uv.Length;
            bool genNorm = verts.Length == norm.Length;
            bool genTan = verts.Length == tan.Length;

            for (int submesh = 0; submesh < submeshCount; submesh++) {
                result.Clear();
                int[] indices = sharedMesh.GetTriangles(submesh);//三角形的三个索引们
                int indicesCount = indices.Length;
                slices[submesh] = new SlicedSubmesh();
                cross.Clear();
                List<Triangle> triangles = new List<Triangle>();//三角形的列表
                List<bool> visited = new List<bool>();
                Dictionary<Line, List<int>> LineTri = new Dictionary<Line, List<int>>(new LineComparer());

                //先对三角形进行遍历，设置好UV等数据后加入三角形列表，方便用点查找
                for (int index = 0; index < indicesCount; index += 3) {
                    int i0 = indices[index + 0];
                    int i1 = indices[index + 1];
                    int i2 = indices[index + 2];
                    //获取索引后从点集找到相应的点组成Triangle
                    Triangle newTri = new Triangle(verts[i0], verts[i1], verts[i2]);
                    newTri.index = index/3;
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
                    //将三角形加入列表，设置访问标记
                    triangles.Add(newTri);
                    visited.Add(false);
                    //先切，一个Cuttting函数，输入三角形、平面，将切割后的CuttingLineAndTri存到result里
                    Intersector.Cutting(pl,newTri,triangles.Count-1,result);
                }

                //对clats中相同的点的标号进行统一
                for(int i = 0; i < result.intersectLines.Count; i++)
                {
                    if (result.intersectLines[i].line.i.Count != 2)
                    {
                        for(int j = i+1; j < result.intersectLines.Count; j++)
                        {
                            if (result.intersectLines[j].line.i.Count != 2 && is_equal(result.intersectLines[i].line.positionA, result.intersectLines[j].line.positionA))
                            {
                                int min = Math.Min(result.intersectLines[i].line.i[0], result.intersectLines[j].line.i[0]);
                                result.intersectLines[i].line.i.Add(min);
                                result.intersectLines[j].line.i.Add(min);
                                break;
                            }
                            if (result.intersectLines[j].line.j.Count != 2 && is_equal(result.intersectLines[i].line.positionA, result.intersectLines[j].line.positionB))
                            {
                                int min = Math.Min(result.intersectLines[i].line.i[0], result.intersectLines[j].line.j[0]);
                                result.intersectLines[i].line.i.Add(min);
                                result.intersectLines[j].line.j.Add(min);
                                break;
                            }
                        }
                    }
                    if (result.intersectLines[i].line.j.Count != 2)
                    {
                        for (int j = i + 1; j < result.intersectLines.Count; j++)
                        {
                            if (result.intersectLines[j].line.i.Count != 2 && is_equal(result.intersectLines[i].line.positionB, result.intersectLines[j].line.positionA))
                            {
                                int min = Math.Min(result.intersectLines[i].line.j[0], result.intersectLines[j].line.i[0]);
                                result.intersectLines[i].line.j.Add(min);
                                result.intersectLines[j].line.i.Add(min);
                                break;
                            }
                            if (result.intersectLines[j].line.j.Count != 2 && is_equal(result.intersectLines[i].line.positionB, result.intersectLines[j].line.positionB))
                            {
                                int min = Math.Min(result.intersectLines[i].line.j[0], result.intersectLines[j].line.j[0]);
                                result.intersectLines[i].line.j.Add(min);
                                result.intersectLines[j].line.j.Add(min);
                                break;
                            }
                        }
                    }
                }

                for(int i = 0; i < result.intersectLines.Count; i++)
                {
                    if (result.intersectLines[i].line.i.Count!=2 || result.intersectLines[i].line.i.Count != 2)
                    {
                        Debug.Log($"{result.intersectLines[i].line.i.Count} {result.intersectLines[i].line.j.Count}");
                    }
                }

                List<CuttingLineAndTri> contour ;
                //线段连成轮廓，判断切哪段,将切面三角剖分并设置uv等
                cross = CaculateContour(result,pl,region,out contour);

                //将被切的三角形进行标记并切割
                for (int i = 0; i < contour.Count(); i++)
                {
                    //切割函数，对三角形集进行添加和标记操作
                    Intersector.ReCutting(pl, contour[i], visited, triangles, result);
                    if (contour[i].TriIndex == -1) { continue; }
                    int index = contour[i].TriIndex;
                    visited[index]=true;
                }

                int sum = 0;
                for(int i = 0; i < visited.Count; i++)
                {
                    if (visited[i] == true) sum++;
                }

                //建立边-三角形映射
                for (int i = 0; i < triangles.Count; i++)
                {
                    Triangle tri = triangles[i];
                    Vector3D a = tri.positionA;
                    Vector3D b = tri.positionB;
                    Vector3D c = tri.positionC;

                    Line[] lines = { new Line(a, b), new Line(b, c), new Line(c, a) };

                    foreach (Line line in lines)
                    {
                        if (!LineTri.TryGetValue(line, out var triList))
                        {
                            LineTri[line] = new List<int>();
                        }
                        LineTri[line].Add(i);
                    }
                }

                //分别搜索两部分
                slices[submesh].upperHull = result.upperTri;
                TriangleSearcher searcher1 = new TriangleSearcher(result.upperTri[0], LineTri, triangles, visited, slices[submesh].upperHull);
                searcher1.StartSearch();
                slices[submesh].lowerHull = result.lowerTri;
                TriangleSearcher searcher2 = new TriangleSearcher(result.lowerTri[0], LineTri, triangles, visited, slices[submesh].lowerHull);
                searcher2.StartSearch();
            }

            for (int i = 0; i < slices.Length; i++) {
                if (slices[i] != null && slices[i].isValid) {
                    //传入上下部分的三角形列表（slice中），截面的交点->截面三角形，切面材质编号
                    return CreateFrom(slices, cross/*用算法计算截面三角形*/, crossIndex);//返回切割后的上下网格在slicehull里
                }
            }

            // no slicing occured, just return null to signify
            return null;
        }

        private static  List<Triangle> CaculateContour(IntersectionResult result, Plane pl, TextureRegion region, out List<CuttingLineAndTri> contour)
        {

            List<CuttingLineAndTri> clats = result.intersectLines;//所有的线段
            HashSet<int> verts = new HashSet<int>();
            contour =new List<CuttingLineAndTri>();//最终的轮廓
            Dictionary<int, List<int>> point2line = new Dictionary<int, List<int>>();//点到线段的映射

            for(int i = 0; i < clats.Count(); i++)//遍历线段，建立点到线段索引的映射，并将所有点加入点集
            {
                //Debug.Log($"{verts.Count} {clats[i].line.i} {clats[i].line.j}");
                if (!point2line.TryGetValue(clats[i].line.i[1], out List<int> value1)) { point2line.Add(clats[i].line.i[1], new List<int>()); verts.Add(clats[i].line.i[1]); }
                point2line[clats[i].line.i[1]].Add(i);
                if (!point2line.TryGetValue(clats[i].line.j[1], out List<int> value2)) { point2line.Add(clats[i].line.j[1], new List<int>()); verts.Add(clats[i].line.j[1]); }
                point2line[clats[i].line.j[1]].Add(i);
            }

            bool[] visited = new bool[clats.Count()];
            List<List<CuttingLineAndTri>> category =new List<List<CuttingLineAndTri>>();
            List<List<Vector3D>> vertexs = new List<List<Vector3D>>();
            double min_dist = double.MaxValue;
            int target = 0;

            while (verts.Count() > 0)
            {
                category.Add(new List<CuttingLineAndTri>());
                vertexs.Add(new List<Vector3D>());
                int pt = verts.First();
                int temp = -1;
                while (verts.Contains(pt))      
                {
                    temp = (!visited[point2line[pt][0]]) ?
                        (clats[point2line[pt][0]].line.i[1] == pt ? clats[point2line[pt][0]].line.j[1] : clats[point2line[pt][0]].line.i[1]) :
                        (clats[point2line[pt][1]].line.i[1] == pt ? clats[point2line[pt][1]].line.j[1] : clats[point2line[pt][1]].line.i[1]);
                    //获取未访问的一边的另一个点
                    int ind = (!visited[point2line[pt][0]]) ? point2line[pt][0] : point2line[pt][1];//获取要访问的线段的索引
                    category[category.Count - 1].Add(clats[ind]);//将访问的线段加入相应的线段集中

                    Vector3D p = clats[ind].line.i[1] == pt ? clats[ind].line.positionA : clats[ind].line.positionB; ;

                    vertexs[vertexs.Count - 1].Add(p);//将点加入相应的点集
                    visited[ind] = true;//将访问过的线段标记
                    double dista = Vector3D.Distance(p, pl.pos);//计算点与平面位置的距离
                    if (dista < min_dist)//取距离最小值，以此确定最内圈的轮廓
                    {
                        min_dist = dista;
                        target = category.Count - 1;
                    }
                    verts.Remove(pt);//删除访问过的点
                    pt= temp;//pt的值变为另一个端点
                }
            }

            if (category.Count > 0) {
                //Debug.Log(category[0].Count);
                contour = category[target];//将轮廓确定为内圈
                for (int i = 0; i < vertexs[target].Count; i++)
                {
                    result.AddIntersectionPoint(vertexs[target][i]);//将轮廓点加入result里的切割点集，并进行三角剖分
                }
                //Debug.Log(vertexs[0].Count);
                //对轮廓进行三角剖分
                return Triangulator.Triangulate(vertexs[target], pl.normal, out List<Triangle> tris, region) ? tris : null;
            }
            
            return null;
        }
        
        //根据上下部分的三角形列表，截面三角形列表，材质编号，返回上下部分的网格
        private static SlicedHull CreateFrom(SlicedSubmesh[] meshes, List<Triangle> cross, int crossSectionIndex) {
            int submeshCount = meshes.Length;

            int upperHullCount = 0;
            int lowerHullCount = 0;

            for (int submesh = 0; submesh < submeshCount; submesh++) {
                upperHullCount += meshes[submesh].upperHull.Count;//所有子网格的上部分三角形数
                lowerHullCount += meshes[submesh].lowerHull.Count;//所有子网格的下部分三角形数
            }

            Mesh upperHull = CreateUpperHull(meshes, upperHullCount, cross, crossSectionIndex);
            Mesh lowerHull = CreateLowerHull(meshes, lowerHullCount, cross, crossSectionIndex);

            return new SlicedHull(upperHull, lowerHull);
        }

        private static Mesh CreateUpperHull(SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossSectionIndex) {
            //Debug.Log("creating upper");
            return CreateHull(mesh, total, crossSection, crossSectionIndex, true);
        }

        private static Mesh CreateLowerHull(SlicedSubmesh[] mesh, int total, List<Triangle> crossSection, int crossSectionIndex) {
            return CreateHull(mesh, total, crossSection, crossSectionIndex, false);
        }

        //根据子网格（只有三角形），此部分总三角形数，截面三角形，截面材质，生成真正的mesh
        private static Mesh CreateHull(SlicedSubmesh[] meshes, int total, List<Triangle> crossSection, int crossIndex, bool isUpper) {
            if (total <= 0) {
                return null;
            }

            int submeshCount = meshes.Length;//子网格数
            int crossCount = crossSection != null ? crossSection.Count : 0;//截面三角形数量

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
                    newVertices[i0] = newTri.positionA.ToVector3();
                    newVertices[i1] = newTri.positionB.ToVector3();
                    newVertices[i2] = newTri.positionC.ToVector3();

                    // add the UV coordinates if any
                    if (hasUV) {
                        newUvs[i0] = newTri.uvA;
                        newUvs[i1] = newTri.uvB;
                        newUvs[i2] = newTri.uvC;
                    }

                    // add the Normals if any
                    if (hasNormal) {
                        newNormals[i0] = newTri.normalA.ToVector3();
                        newNormals[i1] = newTri.normalB.ToVector3();
                        newNormals[i2] = newTri.normalC.ToVector3();
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
            //同上部分，对截面的三角形和点进行处理
            if (crossSection != null && crossCount > 0) {
                int[] crossIndices = new int[crossCount * 3];//截面三角形点的索引

                for (int i = 0, triIndex = 0; i < crossCount; i++, triIndex += 3) {
                    Triangle newTri = crossSection[i];

                    int i0 = vIndex + 0;
                    int i1 = vIndex + 1;
                    int i2 = vIndex + 2;

                    // add the vertices
                    newVertices[i0] = newTri.positionA.ToVector3();
                    newVertices[i1] = newTri.positionB.ToVector3();
                    newVertices[i2] = newTri.positionC.ToVector3();

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
                            newNormals[i0] = -newTri.normalA.ToVector3();
                            newNormals[i1] = -newTri.normalB.ToVector3();
                            newNormals[i2] = -newTri.normalC.ToVector3();
                        } else {
                            newNormals[i0] = newTri.normalA.ToVector3();
                            newNormals[i1] = newTri.normalB.ToVector3();
                            newNormals[i2] = newTri.normalC.ToVector3();
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
    }
}
