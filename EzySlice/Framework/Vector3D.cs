using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ezyslice
{
    public sealed class Vector3D
    {
        public double x;
        public double y;
        public double z;

        private static readonly double epsilon = 1e-5; // 误差容忍度，可调

        public Vector3D(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3D(Vector3 v)
        {
            this.x = (double) v.x;
            this.y = (double) v.y;
            this.z = (double) v.z;
        }

        public Vector3D(Vector3D v)
        {
            this.x = (double)v.x;
            this.y = (double)v.y;
            this.z = (double)v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float) this.x, (float) this.y, (float) this.z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector3D other))
                return false;

            return IsClose(x, other.x) && IsClose(y, other.y) && IsClose(z, other.z);
        }

        private static bool IsClose(double a, double b)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // 将坐标四舍五入到 epsilon 精度倍数，减少误差影响
                long rx = (long)Math.Round(x / epsilon);
                long ry = (long)Math.Round(y / epsilon);
                long rz = (long)Math.Round(z / epsilon);

                int hash = 17;
                hash = hash * 31 + rx.GetHashCode();
                hash = hash * 31 + ry.GetHashCode();
                hash = hash * 31 + rz.GetHashCode();
                return hash;
            }
        }

        public static double Dot(Vector3D a, Vector3D b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static double Distance(Vector3D a, Vector3D b)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // 计算向量的模长（Magnitude）
        public static double Magnitude(Vector3D v)
        {
            return Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        // 归一化向量（Normalize）
        public static Vector3D Normalize(Vector3D v)
        {
            double length = Magnitude(v);
            if (length < 1e-10)  // 避免除零
                throw new InvalidOperationException("Cannot normalize a zero vector.");
            return new Vector3D(v.x / length, v.y / length, v.z / length);
        }

        // 计算两个向量的叉积（Cross Product）
        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        public static void OrthoNormalize(ref Vector3D u, ref Vector3D v)
        {
            // 1. 归一化 u
            u = Normalize(u);

            // 2. 使 v 正交于 u
            v = Normalize(v - u * Dot(v, u));
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        // 重载减法运算符 (Vector - Vector)
        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        // 重载数乘运算符 (Vector * Scalar)
        public static Vector3D operator *(Vector3D v, double scalar)
        {
            return new Vector3D(v.x * scalar, v.y * scalar, v.z * scalar);
        }

        // 重载数乘运算符 (Scalar * Vector) (交换顺序)
        public static Vector3D operator *(double scalar, Vector3D v)
        {
            return new Vector3D(v.x * scalar, v.y * scalar, v.z * scalar);
        }

        // 重载数除运算符 (Vector / Scalar)
        public static Vector3D operator /(Vector3D v, double scalar)
        {
            if (Math.Abs(scalar) < 1e-10)  // 避免除零错误
                throw new DivideByZeroException("Cannot divide by zero.");
            return new Vector3D(v.x / scalar, v.y / scalar, v.z / scalar);
        }

        // 重载一元负号 (取反)
        public static Vector3D operator -(Vector3D v)
        {
            return new Vector3D(-v.x, -v.y, -v.z);
        }
    }
}
