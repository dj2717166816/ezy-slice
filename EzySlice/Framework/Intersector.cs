using Ezyslice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {
    /**
     * Contains static functionality to perform geometric intersection tests.
     */
    public sealed class Intersector {

        public const double Epsilon = 1e-4;

        public static bool Intersect(Plane pl, Vector3D a, Vector3D b, out Vector3D q)
        {
            Vector3D normal = pl.normal;
            Vector3D ab = new Vector3D(b.x - a.x, b.y - a.y, b.z - a.z);

            double t = (pl.dist - Vector3D.Dot(normal, a)) / Vector3D.Dot(normal, ab);

            // need to be careful and compensate for floating errors
            if (t >= -Epsilon && t <= (1 + Epsilon))
            {
                q = new Vector3D(a.x + ab.x * t, a.y + ab.y * t, a.z + ab.z * t);

                return true;
            }

            q = new Vector3D(Vector3.zero);

            return false;
        }

        //计算三点围成三角形面积的二倍的带符号面积，正值说明点ABC是逆时针顺序，表示左转角；反之为顺时针
        public static float TriArea2D(float x1, float y1, float x2, float y2, float x3, float y3) {
            return (x1 - x2) * (y2 - y3) - (x2 - x3) * (y1 - y2);
        }

        //计算切割出的线段，并与三角形编号一同传入CuttingLineAndTri，这里传入的Line中的点顺序可能发生变化，不能直接用在后边切割，要用flip判断是否顺序翻转
        public static void Cutting(Plane pl, Triangle tri, int index, IntersectionResult result)
        {
            //Debug.Log("man");
            Vector3D a = tri.positionA;
            Vector3D b = tri.positionB;
            Vector3D c = tri.positionC;

            SideOfPlane sa = pl.SideOf(a);
            SideOfPlane sb = pl.SideOf(b);
            SideOfPlane sc = pl.SideOf(c);

            //不用切割的情况
            if ((sa == sb && sb == sc) ||
                (sa == SideOfPlane.ON && sb != SideOfPlane.ON && sb == sc) ||
                (sb == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sc) ||
                (sc == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sb))
            {
                return;
            }

            //两个点在平面上，虽然不用切割，但是后边要存并标记
            if (sa == SideOfPlane.ON && sa == sb)
            {
                bool on = sc == SideOfPlane.UP;
                //Debug.Log(1);
                CuttingLineAndTri clat = new CuttingLineAndTri(a, b, -1, 0);
                if (!result.has_clat.ContainsKey(clat))
                {
                    result.has_clat.Add(clat,(index, on));
                }
                else 
                {
                    clat.doubletri[0] = result.has_clat[clat];
                    clat.doubletri[1] = (index, on);
                    result.AddCuttingLine(clat); 
                }
                return;
            }
            if (sa == SideOfPlane.ON && sa == sc)
            {
                bool on = sb == SideOfPlane.UP;
                //Debug.Log(2);
                CuttingLineAndTri clat = new CuttingLineAndTri(a, c, -1, 0);
                if (!result.has_clat.ContainsKey(clat))
                {
                    result.has_clat.Add(clat, (index, on));
                }
                else
                {
                    clat.doubletri[0] = result.has_clat[clat];
                    clat.doubletri[1] = (index, on);
                    result.AddCuttingLine(clat);
                }
                return;
            }
            if (sb == SideOfPlane.ON && sb == sc)
            {
                bool on = sa == SideOfPlane.UP;
                //Debug.Log(3);
                CuttingLineAndTri clat = new CuttingLineAndTri(b, c, -1, 0);
                if (!result.has_clat.ContainsKey(clat))
                {
                    result.has_clat.Add(clat, (index, on));
                }
                else
                {
                    clat.doubletri[0] = result.has_clat[clat];
                    clat.doubletri[1] = (index, on);
                    result.AddCuttingLine(clat);
                }
                return;
            }

            //切割获得两个交点
            Vector3D qa;
            Vector3D qb;

            //一点在平面上，其他两点位于两侧
            //a在平面上
            if (sa == SideOfPlane.ON)
            {
                if (Intersector.Intersect(pl, b, c, out qa))
                {
                    result.AddCuttingLine(new CuttingLineAndTri(qa, a, index, 1));
                }
            }
            //b在平面上
            else if (sb == SideOfPlane.ON)
            {
                if (Intersector.Intersect(pl, a, c, out qa))
                {
                    result.AddCuttingLine(new CuttingLineAndTri(qa, b, index, 2));
                }
            }
            //c在平面上
            else if (sc == SideOfPlane.ON)
            {
                if (Intersector.Intersect(pl, a, b, out qa))
                {
                    result.AddCuttingLine(new CuttingLineAndTri(qa, c, index, 3));
                }
            }

            //一个点都没在平面上
            else if (sa != sb && Intersector.Intersect(pl, a, b, out qa))
            {
                if (sa == sc)
                {
                    if (Intersector.Intersect(pl, b, c, out qb))
                    {
                        result.AddCuttingLine(new CuttingLineAndTri(qa, qb, index, 4));
                    }
                }
                else
                {
                    if (Intersector.Intersect(pl, a, c, out qb))
                    {
                        result.AddCuttingLine(new CuttingLineAndTri(qa, qb, index, 5));
                    }
                }
            }
            else if (Intersector.Intersect(pl, c, a, out qa) && Intersector.Intersect(pl, c, b, out qb))
            {
                result.AddCuttingLine(new CuttingLineAndTri(qa, qb, index, 6));
            }
        } 

        //计算平面切割三角形的结果，并将切割后的三角形标记和添加到原列表，替换被切的三角形
        public static void ReCutting(Plane pl, CuttingLineAndTri contour, List<bool> visited, List<Triangle> triangles, IntersectionResult result)
        {
            if (contour.flag == 0)
            {
                visited[contour.doubletri[0].Item1] = true;
                visited[contour.doubletri[1].Item1] = true;

                if (contour.doubletri[0].Item2) { result.upperTri.Add(triangles[contour.doubletri[0].Item1]); }
                else { result.lowerTri.Add(triangles[contour.doubletri[0].Item1]); }

                if (contour.doubletri[1].Item2) { result.upperTri.Add(triangles[contour.doubletri[1].Item1]); }
                else { result.lowerTri.Add(triangles[contour.doubletri[1].Item1]); }

                return;
            }

            Triangle tri = triangles[contour.TriIndex];
            Vector3D a = tri.positionA;
            Vector3D b = tri.positionB;
            Vector3D c = tri.positionC;
            SideOfPlane sa = pl.SideOf(a);
            SideOfPlane sb = pl.SideOf(b);
            SideOfPlane sc = pl.SideOf(c);

            if (!contour.line.is_fliped)//检查端点顺序是否翻转
            {
                //Debug.Log("sliced1");
                if (contour.flag == 1)
                {
                    //Debug.Log("1");
                    Vector3D qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(a, qa, c);

                    //替换原来的三角形，并添加新三角形，对新三角形进行标记
                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count-1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pa, pq, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pa, pq, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pa, pq, pc);
                    }

                    // b point lies on the upside of the plane
                    if (sb == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // b point lies on the downside of the plane
                    else if (sb == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);//将新三角形加入搜索列表
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 2)
                {
                    //Debug.Log("2");
                    Vector3D qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 3)
                {
                    //Debug.Log("3");
                    Vector3D qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, qa, c);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count-1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pq, pc);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pq, pc);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pq, pc);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 4)
                {
                    //Debug.Log("4");
                    Vector3D qa = contour.line.positionA;
                    Vector3D qb = contour.line.positionB;

                    Triangle ta = new Triangle(qa, b, qb);
                    Triangle tb = new Triangle(a, qa, qb);
                    Triangle tc = new Triangle(a, qb, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pqa, pb, pqb);
                        tb.SetUV(pa, pqa, pqb);
                        tc.SetUV(pa, pqb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pqa, pb, pqb);
                        tb.SetNormal(pa, pqa, pqb);
                        tc.SetNormal(pa, pqb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pb, pqb);
                        tb.SetTangent(pa, pqa, pqb);
                        tc.SetTangent(pa, pqb, pc);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(false);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(tb);
                        //result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                    else
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                        //result.lowerTri.Add(tc);
                    }
                }
                else if (contour.flag == 5)
                {
                    //Debug.Log("5");
                    Vector3D qa = contour.line.positionA;
                    Vector3D qb = contour.line.positionB;

                    Triangle ta = new Triangle(a, qa, qb);
                    Triangle tb = new Triangle(qa, b, c);
                    Triangle tc = new Triangle(qb, qa, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pqa, pqb);
                        tb.SetUV(pqa, pb, pc);
                        tc.SetUV(pqb, pqa, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pqa, pqb);
                        tb.SetNormal(pqa, pb, pc);
                        tc.SetNormal(pqb, pqa, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pqa, pqb);
                        tb.SetTangent(pqa, pb, pc);
                        tc.SetTangent(pqb, pqa, pc);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(false);
                    visited.Add(true);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        //result.lowerTri.Add(tb);
                        result.lowerTri.Add(tc);
                    }
                    else
                    {
                        //result.upperTri.Add(tb);
                        result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                }
                else
                {
                    //Debug.Log("6");
                    Vector3D qa = contour.line.positionA;
                    Vector3D qb = contour.line.positionB;

                    Triangle ta = new Triangle(qa, qb, c);
                    Triangle tb = new Triangle(a, qb, qa);
                    Triangle tc = new Triangle(a, b, qb);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pqa, pqb, pc);
                        tb.SetUV(pa, pqb, pqa);
                        tc.SetUV(pa, pb, pqb);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pqa, pqb, pc);
                        tb.SetNormal(pa, pqb, pqa);
                        tc.SetNormal(pa, pb, pqb);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pqb, pc);
                        tb.SetTangent(pa, pqb, pqa);
                        tc.SetTangent(pa, pb, pqb);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(false);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(tb);
                        //result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                    else
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                        //result.lowerTri.Add(tc);
                    }
                }
            }
            else
            {
                //Debug.Log("sliced2");
                if (contour.flag == 1)
                {
                    //Debug.Log("1");
                    Vector3D qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(a, qa, c);

                    triangles[contour.TriIndex] = ta;
                    ta.index =contour.TriIndex;
                    triangles.Add(tb);
                    tb.index =triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pa, pq, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pa, pq, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pa, pq, pc);
                    }

                    // b point lies on the upside of the plane
                    if (sb == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // b point lies on the downside of the plane
                    else if (sb == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);//将新三角形加入搜索列表
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 2)
                {
                    //Debug.Log("2");
                    Vector3D qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 3)
                {
                    //Debug.Log("3");
                    Vector3D qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, qa, c);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pq, pc);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pq = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pq, pc);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pq, pc);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN)
                    {
                        result.upperTri.Add(tb);
                        result.lowerTri.Add(ta);
                    }
                }
                else if (contour.flag == 4)
                {
                    //Debug.Log("4");
                    Vector3D qa = contour.line.positionB;
                    Vector3D qb = contour.line.positionA;

                    Triangle ta = new Triangle(qa, b, qb);
                    Triangle tb = new Triangle(a, qa, qb);
                    Triangle tc = new Triangle(a, qb, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pqa, pb, pqb);
                        tb.SetUV(pa, pqa, pqb);
                        tc.SetUV(pa, pqb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pqa, pb, pqb);
                        tb.SetNormal(pa, pqa, pqb);
                        tc.SetNormal(pa, pqb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pb, pqb);
                        tb.SetTangent(pa, pqa, pqb);
                        tc.SetTangent(pa, pqb, pc);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(false);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(tb);
                        //result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                    else
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                        //result.lowerTri.Add(tc);
                    }
                }
                else if (contour.flag == 5)
                {
                    //Debug.Log("5");
                    Vector3D qa = contour.line.positionB;
                    Vector3D qb = contour.line.positionA;

                    Triangle ta = new Triangle(a, qa, qb);
                    Triangle tb = new Triangle(qa, b, c);
                    Triangle tc = new Triangle(qb, qa, c);
                    
                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pqa, pqb);
                        tb.SetUV(pqa, pb, pc);
                        tc.SetUV(pqb, pqa, pc);
                    }
                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pa, pqa, pqb);
                        tb.SetNormal(pqa, pb, pc);
                        tc.SetNormal(pqb, pqa, pc);
                    }
                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pqa, pqb);
                        tb.SetTangent(pqa, pb, pc);
                        tc.SetTangent(pqb, pqa, pc);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(false);
                    visited.Add(true);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(ta);
                        //result.lowerTri.Add(tb);
                        result.lowerTri.Add(tc);
                    }
                    else
                    {
                        //result.upperTri.Add(tb);
                        result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                }
                else
                {
                    //Debug.Log("6");
                    Vector3D qa = contour.line.positionB;
                    Vector3D qb = contour.line.positionA;

                    Triangle ta = new Triangle(qa, qb, c);
                    Triangle tb = new Triangle(a, qb, qa);
                    Triangle tc = new Triangle(a, b, qb);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa.ToVector3());
                        Vector2 pqb = tri.GenerateUV(qb.ToVector3());
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pqa, pqb, pc);
                        tb.SetUV(pa, pqb, pqa);
                        tc.SetUV(pa, pb, pqb);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal)
                    {
                        // the computed Normal coordinate if the intersection point
                        Vector3D pqa = tri.GenerateNormal(qa.ToVector3());
                        Vector3D pqb = tri.GenerateNormal(qb.ToVector3());
                        Vector3D pa = tri.normalA;
                        Vector3D pb = tri.normalB;
                        Vector3D pc = tri.normalC;

                        ta.SetNormal(pqa, pqb, pc);
                        tb.SetNormal(pa, pqb, pqa);
                        tc.SetNormal(pa, pb, pqb);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa.ToVector3());
                        Vector4 pqb = tri.GenerateTangent(qb.ToVector3());
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pqb, pc);
                        tb.SetTangent(pa, pqb, pqa);
                        tc.SetTangent(pa, pb, pqb);
                    }

                    triangles[contour.TriIndex] = ta;
                    ta.index = contour.TriIndex;
                    triangles.Add(tb);
                    tb.index = triangles.Count - 1;
                    triangles.Add(tc);
                    tc.index = triangles.Count - 1;
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(false);

                    if (sa == SideOfPlane.UP)
                    {
                        result.upperTri.Add(tb);
                        //result.upperTri.Add(tc);
                        result.lowerTri.Add(ta);
                    }
                    else
                    {
                        result.upperTri.Add(ta);
                        result.lowerTri.Add(tb);
                        //result.lowerTri.Add(tc);
                    }
                }
            }
        }
    }
}