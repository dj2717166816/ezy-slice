using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {

    /**
     * The final generated data structure from a slice operation. This provides easy access
     * to utility functions and the final Mesh data for each section of the HULL.
     */
    public sealed class SlicedHull {
        private Mesh upper_hull;
        private Mesh lower_hull;

        public SlicedHull(Mesh upperHull, Mesh lowerHull) {
            this.upper_hull = upperHull;
            this.lower_hull = lowerHull;
        }

        public GameObject CreateUpperHull(GameObject original) {
            return CreateUpperHull(original, null);
        }

        public GameObject CreateUpperHull(GameObject original, Material crossSectionMat) {
            GameObject newObject = CreateUpperHull();

            if (newObject != null) {
                newObject.transform.localPosition = original.transform.localPosition;
                newObject.transform.localRotation = original.transform.localRotation;
                newObject.transform.localScale = original.transform.localScale;//切割后的部分保持和原物体同位置旋转和缩放

                Material[] shared = { original.GetComponent<MeshRenderer>().sharedMaterials[0] };
                Mesh mesh = original.GetComponent<MeshFilter>().sharedMesh;

                //// nothing changed in the hierarchy, the cross section must have been batched
                //// with the submeshes, return as is, no need for any changes
                //if (mesh.subMeshCount == upper_hull.subMeshCount) {//如果没有多出来的子网格
                //    // the the material information
                //    newObject.GetComponent<Renderer>().sharedMaterials = shared;

                //    return newObject;
                //}

                //// otherwise the cross section was added to the back of the submesh array because
                //// it uses a different material. We need to take this into account
                //Material[] newShared = new Material[shared.Length + 1];

                //// copy our material arrays across using native copy (should be faster than loop)
                //System.Array.Copy(shared, newShared, shared.Length);//复制之前的材质
                //newShared[shared.Length] = crossSectionMat;//添加新截面材质

                //// the the material information
                //newObject.GetComponent<Renderer>().sharedMaterials = newShared;

                newObject.GetComponent<Renderer>().sharedMaterials = shared;

                return newObject;
            }

            return newObject;
        }

        public GameObject CreateLowerHull(GameObject original) {
            return CreateLowerHull(original, null);
        }

        public GameObject CreateLowerHull(GameObject original, Material crossSectionMat) {
            GameObject newObject = CreateLowerHull();

            if (newObject != null) {
                newObject.transform.localPosition = original.transform.localPosition;
                newObject.transform.localRotation = original.transform.localRotation;
                newObject.transform.localScale = original.transform.localScale;

                Material[] shared = { original.GetComponent<MeshRenderer>().sharedMaterials[0] };
                Mesh mesh = original.GetComponent<MeshFilter>().sharedMesh;

                //// nothing changed in the hierarchy, the cross section must have been batched
                //// with the submeshes, return as is, no need for any changes
                //if (mesh.subMeshCount == lower_hull.subMeshCount) {
                //    // the the material information
                //    newObject.GetComponent<Renderer>().sharedMaterials = shared;

                //    return newObject;
                //}

                //// otherwise the cross section was added to the back of the submesh array because
                //// it uses a different material. We need to take this into account
                //Material[] newShared = new Material[shared.Length + 1];

                //// copy our material arrays across using native copy (should be faster than loop)
                //System.Array.Copy(shared, newShared, shared.Length);
                //newShared[shared.Length] = crossSectionMat;

                //// the the material information
                //newObject.GetComponent<Renderer>().sharedMaterials = newShared;

                newObject.GetComponent<Renderer>().sharedMaterials = shared;

                return newObject;
            }

            return newObject;
        }

        /**
         * Generate a new GameObject from the upper hull of the mesh
         * This function will return null if upper hull does not exist
         */
        public GameObject CreateUpperHull() {
            return CreateEmptyObject("Upper_Hull", upper_hull);
        }

        /**
         * Generate a new GameObject from the Lower hull of the mesh
         * This function will return null if lower hull does not exist
         */
        public GameObject CreateLowerHull() {
            return CreateEmptyObject("Lower_Hull", lower_hull);
        }

        public Mesh upperHull {
            get { return this.upper_hull; }
        }

        public Mesh lowerHull {
            get { return this.lower_hull; }
        }

        /**
         * Helper function which will create a new GameObject to be able to add
         * a new mesh for rendering and return.
         */
        //创建一个名字为name的带网格物体
        private static GameObject CreateEmptyObject(string name, Mesh hull) {
            if (hull == null) {
                return null;
            }

            GameObject newObject = new GameObject(name);

            newObject.AddComponent<MeshRenderer>();
            MeshFilter filter = newObject.AddComponent<MeshFilter>();

            MergeSubMeshes(hull);

            filter.mesh = hull;

            return newObject;
        }
        private static void MergeSubMeshes(Mesh mesh)
        {
            if (mesh.subMeshCount <= 1) return;

            List<int> mergedTriangles = new List<int>();

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] tris = mesh.GetTriangles(i);
                mergedTriangles.AddRange(tris);
            }

            mesh.subMeshCount = 1;
            mesh.SetTriangles(mergedTriangles, 0);
        }

    }
}