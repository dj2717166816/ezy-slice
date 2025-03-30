using System.Collections;
using UnityEngine;
using Ezyslice;

namespace EzySlice
{
    /**
     * Define Extension methods for easy access to slicer functionality
     */
    public static class SlicerExtensions
    {

        /**
         * SlicedHull Return functions and appropriate overrides!
         */
        //输入物体、平面、材质
        public static SlicedHull Slice(this GameObject obj, Plane pl, Material crossSectionMaterial = null)
        {
            return Slice(obj, pl, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), crossSectionMaterial);
        }
        //输入物体，平面上一点，法线，材质
        public static SlicedHull Slice(this GameObject obj, Vector3 position, Vector3 direction, Material crossSectionMaterial = null)
        {
            return Slice(obj, position, direction, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), crossSectionMaterial);
        }
        //输入物体，平面上一点，法线，纹理范围，材质
        public static SlicedHull Slice(this GameObject obj, Vector3 position, Vector3 direction, TextureRegion textureRegion, Material crossSectionMaterial = null)
        {
            Plane cuttingPlane = new Plane();

            Matrix4x4 mat = obj.transform.worldToLocalMatrix;
            Matrix4x4 transpose = mat.transpose;
            Matrix4x4 inv = transpose.inverse;

            Vector3D refUp = new Vector3D(inv.MultiplyVector(direction).normalized);
            Vector3D refPt = new Vector3D(obj.transform.InverseTransformPoint(position));

            cuttingPlane.Compute(refPt, refUp);

            return Slice(obj, cuttingPlane, textureRegion, crossSectionMaterial);
        }
        //输入物体，平面，纹理范围，材质
        public static SlicedHull Slice(this GameObject obj, Plane pl, TextureRegion textureRegion, Material crossSectionMaterial = null)
        {
            return Slicer.Slice(obj, pl, textureRegion, crossSectionMaterial);
        }

        /**
         * These functions (and overrides) will return the final indtaniated GameObjects types
         */
        public static GameObject[] SliceInstantiate(this GameObject obj, Plane pl)
        {
            return SliceInstantiate(obj, pl, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f));
        }

        public static GameObject[] SliceInstantiate(this GameObject obj, Vector3 position, Vector3 direction)
        {
            return SliceInstantiate(obj, position, direction, null);
        }

        public static GameObject[] SliceInstantiate(this GameObject obj, Vector3 position, Vector3 direction, Material crossSectionMat)
        {
            return SliceInstantiate(obj, position, direction, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), crossSectionMat);
        }

        public static GameObject[] SliceInstantiate(this GameObject obj, Vector3 position, Vector3 direction, TextureRegion cuttingRegion, Material crossSectionMaterial = null)
        {
            EzySlice.Plane cuttingPlane = new EzySlice.Plane();

            //将世界坐标系转换为局部坐标系
            Matrix4x4 mat = obj.transform.worldToLocalMatrix;
            Matrix4x4 transpose = mat.transpose;
            Matrix4x4 inv = transpose.inverse;

            Vector3D refUp = new Vector3D(inv.MultiplyVector(direction).normalized);
            Vector3D refPt = new Vector3D(obj.transform.InverseTransformPoint(position));

            cuttingPlane.Compute(refPt, refUp);

            return SliceInstantiate(obj, cuttingPlane, cuttingRegion, crossSectionMaterial);
        }

        public static GameObject[] SliceInstantiate(this GameObject obj, Plane pl, TextureRegion cuttingRegion, Material crossSectionMaterial = null)
        {
            SlicedHull slice = Slicer.Slice(obj, pl, cuttingRegion, crossSectionMaterial);

            if (slice == null)
            {
                return null;
            }

            GameObject upperHull = slice.CreateUpperHull(obj, crossSectionMaterial);
            GameObject lowerHull = slice.CreateLowerHull(obj, crossSectionMaterial);

            if (upperHull != null && lowerHull != null)
            {
                return new GameObject[] { upperHull, lowerHull };
            }

            // otherwise return only the upper hull
            if (upperHull != null)
            {
                return new GameObject[] { upperHull };
            }

            // otherwise return only the lower hull
            if (lowerHull != null)
            {
                return new GameObject[] { lowerHull };
            }

            // nothing to return, so return nothing!
            return null;
        }
    }
}