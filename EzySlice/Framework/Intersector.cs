using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {
    /**
     * Contains static functionality to perform geometric intersection tests.
     */
    public sealed class Intersector {

        public const float Epsilon = 0.0001f;

        public static bool Intersect(Plane pl, Vector3 a, Vector3 b, out Vector3 q) {
            Vector3 normal = pl.normal;
            Vector3 ab = b - a;

            float t = (pl.dist - Vector3.Dot(normal, a)) / Vector3.Dot(normal, ab);

            // need to be careful and compensate for floating errors
            if (t >= -Epsilon && t <= (1 + Epsilon)) {
                q = a + t * ab;

                return true;
            }

            q = Vector3.zero;

            return false;
        }

        //计算三点围成三角形面积的二倍的带符号面积，正值说明点ABC是逆时针顺序，表示左转角；反之为顺时针
        public static float TriArea2D(float x1, float y1, float x2, float y2, float x3, float y3) {
            return (x1 - x2) * (y2 - y3) - (x2 - x3) * (y1 - y2);
        }

        //计算切割出的线段，并与三角形编号一同传入CuttingLineAndTri，这里传入的Line中的点顺序可能发生变化，不能直接用在后边切割，要用flip判断是否顺序翻转
        public static void Cutting(Plane pl, Triangle tri, int index, IntersectionResult result)
        {
            Vector3 a = tri.positionA;
            Vector3 b = tri.positionB;
            Vector3 c = tri.positionC;

            SideOfPlane sa = pl.SideOf(a);
            SideOfPlane sb = pl.SideOf(b);
            SideOfPlane sc = pl.SideOf(c);

            //两个点在平面上，虽然不用切割，但是后边要存并标记
            if (sa == SideOfPlane.ON && sa == sb)
            {
                bool on = sc == SideOfPlane.ON;

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
                bool on = sb == SideOfPlane.ON;

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
                bool on = sa == SideOfPlane.ON;

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

            //不用切割的情况
            if ((sa == sb && sb == sc)||
                ((sa == SideOfPlane.ON && sb != SideOfPlane.ON && sb == sc) ||(sb == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sc) || (sc == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sb)))
            {
                return;
            }

            //切割获得两个交点
            Vector3 qa;
            Vector3 qb;

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
            Vector3 a = tri.positionA;
            Vector3 b = tri.positionB;
            Vector3 c = tri.positionC;
            SideOfPlane sa = pl.SideOf(a);
            SideOfPlane sb = pl.SideOf(b);
            SideOfPlane sc = pl.SideOf(c);

            if (!contour.line.is_fliped)//检查端点顺序是否翻转
            {
                //Debug.Log("sliced1");
                if (contour.flag == 1)
                {
                    //Debug.Log("1");
                    Vector3 qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(a, qa, c);

                    //替换原来的三角形，并添加新三角形，对新三角形进行标记
                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pa, pq, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionA;

                    Triangle ta = new Triangle(a, qa, c);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pq, pc);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionA;
                    Vector3 qb = contour.line.positionB;

                    Triangle ta = new Triangle(qa, b, qb);
                    Triangle tb = new Triangle(a, qa, qb);
                    Triangle tc = new Triangle(a, qb, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pqa, pb, pqb);
                        tb.SetNormal(pa, pqa, pqb);
                        tc.SetNormal(pa, pqb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pb, pqb);
                        tb.SetTangent(pa, pqa, pqb);
                        tc.SetTangent(pa, pqb, pc);
                    }

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
                    Vector3 qa = contour.line.positionA;
                    Vector3 qb = contour.line.positionB;

                    Triangle ta = new Triangle(a, qa, qb);
                    Triangle tb = new Triangle(qa, b, c);
                    Triangle tc = new Triangle(qb, qa, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pqa, pqb);
                        tb.SetNormal(pqa, pb, pc);
                        tc.SetNormal(pqb, pqa, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pqa, pqb);
                        tb.SetTangent(pqa, pb, pc);
                        tc.SetTangent(pqb, pqa, pc);
                    }

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
                    Vector3 qa = contour.line.positionA;
                    Vector3 qb = contour.line.positionB;

                    Triangle ta = new Triangle(qa, qb, c);
                    Triangle tb = new Triangle(a, qb, qa);
                    Triangle tc = new Triangle(a, b, qb);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pqa, pqb, pc);
                        tb.SetNormal(pa, pqb, pqa);
                        tc.SetNormal(pa, pb, pqb);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pqb, pc);
                        tb.SetTangent(pa, pqb, pqa);
                        tc.SetTangent(pa, pb, pqb);
                    }

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
                    Vector3 qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(a, qa, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pa, pq, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionB;

                    Triangle ta = new Triangle(a, qa, c);
                    Triangle tb = new Triangle(qa, b, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
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
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pq, pc);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
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
                    Vector3 qa = contour.line.positionB;
                    Vector3 qb = contour.line.positionA;

                    Triangle ta = new Triangle(qa, b, qb);
                    Triangle tb = new Triangle(a, qa, qb);
                    Triangle tc = new Triangle(a, qb, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pqa, pb, pqb);
                        tb.SetNormal(pa, pqa, pqb);
                        tc.SetNormal(pa, pqb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pb, pqb);
                        tb.SetTangent(pa, pqa, pqb);
                        tc.SetTangent(pa, pqb, pc);
                    }

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
                    Vector3 qa = contour.line.positionB;
                    Vector3 qb = contour.line.positionA;

                    Triangle ta = new Triangle(a, qa, qb);
                    Triangle tb = new Triangle(qa, b, c);
                    Triangle tc = new Triangle(qb, qa, c);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);
                    
                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pqa, pqb);
                        tb.SetNormal(pqa, pb, pc);
                        tc.SetNormal(pqb, pqa, pc);
                    }
                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pqa, pqb);
                        tb.SetTangent(pqa, pb, pc);
                        tc.SetTangent(pqb, pqa, pc);
                    }
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
                    Vector3 qa = contour.line.positionB;
                    Vector3 qb = contour.line.positionA;

                    Triangle ta = new Triangle(qa, qb, c);
                    Triangle tb = new Triangle(a, qb, qa);
                    Triangle tc = new Triangle(a, b, qb);

                    triangles[contour.TriIndex] = ta;
                    triangles.Add(tb);
                    triangles.Add(tc);
                    visited[contour.TriIndex] = true;
                    visited.Add(true);
                    visited.Add(true);

                    // generate UV coordinates if there is any
                    if (tri.hasUV)
                    {
                        // the computed UV coordinate if the intersection point
                        Vector2 pqa = tri.GenerateUV(qa);
                        Vector2 pqb = tri.GenerateUV(qb);
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
                        Vector3 pqa = tri.GenerateNormal(qa);
                        Vector3 pqb = tri.GenerateNormal(qb);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pqa, pqb, pc);
                        tb.SetNormal(pa, pqb, pqa);
                        tc.SetNormal(pa, pb, pqb);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent)
                    {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pqa = tri.GenerateTangent(qa);
                        Vector4 pqb = tri.GenerateTangent(qb);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pqa, pqb, pc);
                        tb.SetTangent(pa, pqb, pqa);
                        tc.SetTangent(pa, pb, pqb);
                    }

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

        public static void Search(Triangle tri, Dictionary<Line, int[]> LineTri, List<Triangle> triangles, List<bool> visited, List<Triangle> output)
        {
            return;
            Vector3 a = tri.positionA;
            Vector3 b = tri.positionB;
            Vector3 c = tri.positionC;
            Line line1 = new Line(a, b);
            Line line2 = new Line(b, c);
            Line line3 = new Line(c, a);
            Line[] lines = { line1, line2, line3 };
            for (int i = 0; i < lines.Length; i++)
            {
                Line line = lines[i];
                if (Triangle.AreTrianglesEqual(triangles[LineTri[line][0]], tri))
                {
                    if (!visited[LineTri[line][1]])
                    {
                        visited[LineTri[line][1]] = true;
                        output.Add(triangles[LineTri[line][1]]);
                        Search(triangles[LineTri[line][1]], LineTri, triangles, visited, output);
                    }
                }
                else
                {
                    if (!visited[LineTri[line][0]])
                    {
                        visited[LineTri[line][0]] = true;
                        output.Add(triangles[LineTri[line][0]]);
                        Search(triangles[LineTri[line][0]], LineTri, triangles, visited, output);
                    }
                }
            }
        }
        /*public static void Intersect(Plane pl, Triangle tri, IntersectionResult result) {
            // clear the previous results from the IntersectionResult
            //result.Clear();

            // grab local variables for easier access
            Vector3 a = tri.positionA;
            Vector3 b = tri.positionB;
            Vector3 c = tri.positionC;

            // check to see which side of the plane the points all
            // lay in. SideOf operation is a simple dot product and some comparison
            // operations, so these are a very quick checks
            //判断点与平面的关系
            SideOfPlane sa = pl.SideOf(a);
            SideOfPlane sb = pl.SideOf(b);
            SideOfPlane sc = pl.SideOf(c);

            // we cannot intersect if the triangle points all fall on the same side
            // of the plane. This is an easy early out test as no intersections are possible.
            //全在一侧
            if (sa == sb && sb == sc) {
                return;
            }

            // detect cases where two points lay straight on the plane, meaning
            // that the plane is actually parralel with one of the edges of the triangle
            //一个点在平面上，其他在一侧
            else if ((sa == SideOfPlane.ON && sa == sb) ||
                (sa == SideOfPlane.ON && sa == sc) ||
                (sb == SideOfPlane.ON && sb == sc)) {
                return;
            }
            
            // detect cases where one point is on the plane and the other two are on the same side
            //两个点在平面上
            else if ((sa == SideOfPlane.ON && sb != SideOfPlane.ON && sb == sc) ||
                     (sb == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sc) ||
                     (sc == SideOfPlane.ON && sa != SideOfPlane.ON && sa == sb)) {
                return;
            }

            // keep in mind that intersection points are shared by both
            // the upper HULL and lower HULL hence they lie perfectly
            // on the plane that cut them

//此处可判断两交点是否属于同一三角形，从而在截面连接时判定
            //切割三角形得到两个交点
            Vector3 qa;
            Vector3 qb;                           

            // check the cases where the points of the triangle actually lie on the plane itself
            // in these cases, there is only going to be 2 triangles, one for the upper HULL and
            // the other on the lower HULL
            // we just need to figure out which points to accept into the upper or lower hulls.
            //a在平面上
            if (sa == SideOfPlane.ON) {//一点在平面上，其他两点位于两侧
                // if the point a is on the plane, test line b-c
                if (Intersector.Intersect(pl, b, c, out qa)) {
                    // line b-c intersected, construct out triangles and return approprietly
                    result.AddIntersectionPoint(qa);
                    result.AddIntersectionPoint(a);

                    // our two generated triangles, we need to figure out which
                    // triangle goes into the UPPER hull and which goes into the LOWER hull
                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(a, qa, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV) {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pa, pq, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal) {
                        // the computed Normal coordinate if the intersection point
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pa, pq, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent) {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pa, pq, pc);
                    }

                    // b point lies on the upside of the plane
                    if (sb == SideOfPlane.UP) {
                        result.AddUpperHull(ta).AddLowerHull(tb);
                    }

                    // b point lies on the downside of the plane
                    else if (sb == SideOfPlane.DOWN) {
                        result.AddUpperHull(tb).AddLowerHull(ta);
                    }
                }
            }

            // test the case where the b point lies on the plane itself
            //b在平面上
            else if (sb == SideOfPlane.ON) {
                // if the point b is on the plane, test line a-c
                if (Intersector.Intersect(pl, a, c, out qa)) {
                    // line a-c intersected, construct out triangles and return approprietly
                    result.AddIntersectionPoint(qa);
                    result.AddIntersectionPoint(b);

                    // our two generated triangles, we need to figure out which
                    // triangle goes into the UPPER hull and which goes into the LOWER hull
                    Triangle ta = new Triangle(a, b, qa);
                    Triangle tb = new Triangle(qa, b, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV) {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pb, pq);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal) {
                        // the computed Normal coordinate if the intersection point
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pb, pq);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent) {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pb, pq);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP) {
                        result.AddUpperHull(ta).AddLowerHull(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN) {
                        result.AddUpperHull(tb).AddLowerHull(ta);
                    }
                }
            }

            // test the case where the c point lies on the plane itself
            //c在平面上
            else if (sc == SideOfPlane.ON) {
                // if the point c is on the plane, test line a-b
                if (Intersector.Intersect(pl, a, b, out qa)) {
                    // line a-c intersected, construct out triangles and return approprietly
                    result.AddIntersectionPoint(qa);
                    result.AddIntersectionPoint(c);

                    // our two generated triangles, we need to figure out which
                    // triangle goes into the UPPER hull and which goes into the LOWER hull
                    Triangle ta = new Triangle(a, qa, c);
                    Triangle tb = new Triangle(qa, b, c);

                    // generate UV coordinates if there is any
                    if (tri.hasUV) {
                        // the computed UV coordinate if the intersection point
                        Vector2 pq = tri.GenerateUV(qa);
                        Vector2 pa = tri.uvA;
                        Vector2 pb = tri.uvB;
                        Vector2 pc = tri.uvC;

                        ta.SetUV(pa, pq, pc);
                        tb.SetUV(pq, pb, pc);
                    }

                    // generate Normal coordinates if there is any
                    if (tri.hasNormal) {
                        // the computed Normal coordinate if the intersection point
                        Vector3 pq = tri.GenerateNormal(qa);
                        Vector3 pa = tri.normalA;
                        Vector3 pb = tri.normalB;
                        Vector3 pc = tri.normalC;

                        ta.SetNormal(pa, pq, pc);
                        tb.SetNormal(pq, pb, pc);
                    }

                    // generate Tangent coordinates if there is any
                    if (tri.hasTangent) {
                        // the computed Tangent coordinate if the intersection point
                        Vector4 pq = tri.GenerateTangent(qa);
                        Vector4 pa = tri.tangentA;
                        Vector4 pb = tri.tangentB;
                        Vector4 pc = tri.tangentC;

                        ta.SetTangent(pa, pq, pc);
                        tb.SetTangent(pq, pb, pc);
                    }

                    // a point lies on the upside of the plane
                    if (sa == SideOfPlane.UP) {
                        result.AddUpperHull(ta).AddLowerHull(tb);
                    }

                    // a point lies on the downside of the plane
                    else if (sa == SideOfPlane.DOWN) {
                        result.AddUpperHull(tb).AddLowerHull(ta);
                    }
                }
            }

            // at this point, all edge cases have been tested and failed, we need to perform
            // full intersection tests against the lines. From this point onwards we will generate
            // 3 triangles
            //一个点都没在平面上

            else if (sa != sb && Intersector.Intersect(pl, a, b, out qa)) {
                // intersection found against a - b
                result.AddIntersectionPoint(qa);

                // since intersection was found against a - b, we need to check which other
                // lines to check (we only need to check one more line) for intersection.
                // the line we check against will be the line against the point which lies on
                // the other side of the plane.
                if (sa == sc) {
                    // we likely have an intersection against line b-c which will complete this loop
                    if (Intersector.Intersect(pl, b, c, out qb)) {
                        result.AddIntersectionPoint(qb);

                        // our three generated triangles. Two of these triangles will end
                        // up on either the UPPER or LOWER hulls.
                        Triangle ta = new Triangle(qa, b, qb);
                        Triangle tb = new Triangle(a, qa, qb);
                        Triangle tc = new Triangle(a, qb, c);

                        // generate UV coordinates if there is any
                        if (tri.hasUV) {
                            // the computed UV coordinate if the intersection point
                            Vector2 pqa = tri.GenerateUV(qa);
                            Vector2 pqb = tri.GenerateUV(qb);
                            Vector2 pa = tri.uvA;
                            Vector2 pb = tri.uvB;
                            Vector2 pc = tri.uvC;

                            ta.SetUV(pqa, pb, pqb);
                            tb.SetUV(pa, pqa, pqb);
                            tc.SetUV(pa, pqb, pc);
                        }

                        // generate Normal coordinates if there is any
                        if (tri.hasNormal) {
                            // the computed Normal coordinate if the intersection point
                            Vector3 pqa = tri.GenerateNormal(qa);
                            Vector3 pqb = tri.GenerateNormal(qb);
                            Vector3 pa = tri.normalA;
                            Vector3 pb = tri.normalB;
                            Vector3 pc = tri.normalC;

                            ta.SetNormal(pqa, pb, pqb);
                            tb.SetNormal(pa, pqa, pqb);
                            tc.SetNormal(pa, pqb, pc);
                        }

                        // generate Tangent coordinates if there is any
                        if (tri.hasTangent) {
                            // the computed Tangent coordinate if the intersection point
                            Vector4 pqa = tri.GenerateTangent(qa);
                            Vector4 pqb = tri.GenerateTangent(qb);
                            Vector4 pa = tri.tangentA;
                            Vector4 pb = tri.tangentB;
                            Vector4 pc = tri.tangentC;

                            ta.SetTangent(pqa, pb, pqb);
                            tb.SetTangent(pa, pqa, pqb);
                            tc.SetTangent(pa, pqb, pc);
                        }

                        if (sa == SideOfPlane.UP) {
                            result.AddUpperHull(tb).AddUpperHull(tc).AddLowerHull(ta);
                        } else {
                            result.AddLowerHull(tb).AddLowerHull(tc).AddUpperHull(ta);
                        }
                    }
                } else {
                    // in this scenario, the point a is a "lone" point which lies in either upper
                    // or lower HULL. We need to perform another intersection to find the last point
                    if (Intersector.Intersect(pl, a, c, out qb)) {
                        result.AddIntersectionPoint(qb);

                        // our three generated triangles. Two of these triangles will end
                        // up on either the UPPER or LOWER hulls.
                        Triangle ta = new Triangle(a, qa, qb);
                        Triangle tb = new Triangle(qa, b, c);
                        Triangle tc = new Triangle(qb, qa, c);

                        // generate UV coordinates if there is any
                        if (tri.hasUV) {
                            // the computed UV coordinate if the intersection point
                            Vector2 pqa = tri.GenerateUV(qa);
                            Vector2 pqb = tri.GenerateUV(qb);
                            Vector2 pa = tri.uvA;
                            Vector2 pb = tri.uvB;
                            Vector2 pc = tri.uvC;

                            ta.SetUV(pa, pqa, pqb);
                            tb.SetUV(pqa, pb, pc);
                            tc.SetUV(pqb, pqa, pc);
                        }

                        // generate Normal coordinates if there is any
                        if (tri.hasNormal) {
                            // the computed Normal coordinate if the intersection point
                            Vector3 pqa = tri.GenerateNormal(qa);
                            Vector3 pqb = tri.GenerateNormal(qb);
                            Vector3 pa = tri.normalA;
                            Vector3 pb = tri.normalB;
                            Vector3 pc = tri.normalC;

                            ta.SetNormal(pa, pqa, pqb);
                            tb.SetNormal(pqa, pb, pc);
                            tc.SetNormal(pqb, pqa, pc);
                        }

                        // generate Tangent coordinates if there is any
                        if (tri.hasTangent) {
                            // the computed Tangent coordinate if the intersection point
                            Vector4 pqa = tri.GenerateTangent(qa);
                            Vector4 pqb = tri.GenerateTangent(qb);
                            Vector4 pa = tri.tangentA;
                            Vector4 pb = tri.tangentB;
                            Vector4 pc = tri.tangentC;

                            ta.SetTangent(pa, pqa, pqb);
                            tb.SetTangent(pqa, pb, pc);
                            tc.SetTangent(pqb, pqa, pc);
                        }

                        if (sa == SideOfPlane.UP) {
                            result.AddUpperHull(ta).AddLowerHull(tb).AddLowerHull(tc);
                        } else {
                            result.AddLowerHull(ta).AddUpperHull(tb).AddUpperHull(tc);
                        }
                    }
                }
            }

            // if line a-b did not intersect (or the lie on the same side of the plane)
            // this simplifies the problem a fair bit. This means we have an intersection 
            // in line a-c and b-c, which we can use to build a new UPPER and LOWER hulls
            // we are expecting both of these intersection tests to pass, otherwise something
            // went wrong (float errors? missed a checked case?)
            else if (Intersector.Intersect(pl, c, a, out qa) && Intersector.Intersect(pl, c, b, out qb)) {
                // in here we know that line a-b actually lie on the same side of the plane, this will
                // simplify the rest of the logic. We also have our intersection points
                // the computed UV coordinate of the intersection point

                result.AddIntersectionPoint(qa);
                result.AddIntersectionPoint(qb);

                // our three generated triangles. Two of these triangles will end
                // up on either the UPPER or LOWER hulls.
                Triangle ta = new Triangle(qa, qb, c);
                Triangle tb = new Triangle(a, qb, qa);
                Triangle tc = new Triangle(a, b, qb);

                // generate UV coordinates if there is any
                if (tri.hasUV) {
                    // the computed UV coordinate if the intersection point
                    Vector2 pqa = tri.GenerateUV(qa);
                    Vector2 pqb = tri.GenerateUV(qb);
                    Vector2 pa = tri.uvA;
                    Vector2 pb = tri.uvB;
                    Vector2 pc = tri.uvC;

                    ta.SetUV(pqa, pqb, pc);
                    tb.SetUV(pa, pqb, pqa);
                    tc.SetUV(pa, pb, pqb);
                }

                // generate Normal coordinates if there is any
                if (tri.hasNormal) {
                    // the computed Normal coordinate if the intersection point
                    Vector3 pqa = tri.GenerateNormal(qa);
                    Vector3 pqb = tri.GenerateNormal(qb);
                    Vector3 pa = tri.normalA;
                    Vector3 pb = tri.normalB;
                    Vector3 pc = tri.normalC;

                    ta.SetNormal(pqa, pqb, pc);
                    tb.SetNormal(pa, pqb, pqa);
                    tc.SetNormal(pa, pb, pqb);
                }

                // generate Tangent coordinates if there is any
                if (tri.hasTangent) {
                    // the computed Tangent coordinate if the intersection point
                    Vector4 pqa = tri.GenerateTangent(qa);
                    Vector4 pqb = tri.GenerateTangent(qb);
                    Vector4 pa = tri.tangentA;
                    Vector4 pb = tri.tangentB;
                    Vector4 pc = tri.tangentC;

                    ta.SetTangent(pqa, pqb, pc);
                    tb.SetTangent(pa, pqb, pqa);
                    tc.SetTangent(pa, pb, pqb);
                }

                if (sa == SideOfPlane.UP) {
                    result.AddUpperHull(tb).AddUpperHull(tc).AddLowerHull(ta);
                } else {
                    result.AddLowerHull(tb).AddLowerHull(tc).AddUpperHull(ta);
                }
            }
        }*/
    }
}