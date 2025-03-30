using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Ezyslice;

namespace EzySlice {

    /**
     * Quick Internal structure which checks where the point lays on the
     * Plane. UP = Upwards from the Normal, DOWN = Downwards from the Normal
     * ON = Point lays straight on the plane
     */
    public enum SideOfPlane {
        UP,
        DOWN,
        ON
    }

    /**
     * Represents a simple 3D Plane structure with a position
     * and direction which extends infinitely in its axis. This provides
     * an optimal structure for collision tests for the slicing framework.
     */
    public struct Plane {
        private Vector3D m_normal;
        private Vector3D m_pos;
        private double m_dist;

        public Plane(Vector3D pos, Vector3D norm) {
            this.m_normal = norm;
            this.m_dist = Vector3D.Dot(norm, pos);
            this.m_pos=pos;
        }

        public void Compute(Vector3D pos, Vector3D norm) {
            this.m_normal = norm;
            this.m_dist = Vector3D.Dot(norm, pos);
        }

        public void Compute(Transform trans) {
            Compute(new Vector3D(trans.position), new Vector3D(trans.up));
        }

        public void Compute(GameObject obj) {
            Compute(obj.transform);
        }

        public Vector3D normal {
            get { return this.m_normal; }
        }

        public double dist {
            get { return this.m_dist; }
        }

        public Vector3D pos{
            get { return this.m_pos; }
        }

        public SideOfPlane SideOf(Vector3D pt) {
            double result = Vector3D.Dot(m_normal, pt) - m_dist;

            if (result > Intersector.Epsilon) {
                return SideOfPlane.UP;
            }

            if (result < -Intersector.Epsilon) {
                return SideOfPlane.DOWN;
            }

            return SideOfPlane.ON;
        }
    }
}
