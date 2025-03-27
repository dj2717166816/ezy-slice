using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {
    public struct Line {
        private readonly Vector3 m_pos_a;
        private readonly Vector3 m_pos_b;
        private readonly bool flip;
        public Line(Vector3 a, Vector3 b)
        {
            // 通过坐标大小比较来保证无向性
            if (a.x < b.x || (a.x == b.x && a.y < b.y) || (a.x == b.x && a.y == b.y && a.z < b.z))
            {
                this.m_pos_a = a;
                this.m_pos_b = b;
                this.flip = false;
            }
            else
            {
                this.m_pos_a = b;
                this.m_pos_b = a;
                this.flip = true;
            }
        }

        public bool Equals(Line other)
        {
            return (m_pos_a == other.m_pos_a && m_pos_b == other.m_pos_b);
        }
        public override bool Equals(object obj)
        {
            if (obj is Line other)
            {
                return (m_pos_a == other.m_pos_a && m_pos_b == other.m_pos_b) ||
                       (m_pos_a == other.m_pos_b && m_pos_b == other.m_pos_a); // 处理无向边
            }
            return false;
        }
        public override int GetHashCode()
        {
            return m_pos_a.GetHashCode() ^ m_pos_b.GetHashCode();
        }
        public bool is_fliped
        {
            get { return flip; }
        }

        public Vector3 positionA {
            get { return this.m_pos_a; }
        }

        public Vector3 positionB {
            get { return this.m_pos_b; }
        }
    }
}