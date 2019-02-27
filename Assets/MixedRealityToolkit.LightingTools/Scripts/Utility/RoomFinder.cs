using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class VecCmp : IComparer<Vector3> { public int Compare(Vector3 a, Vector3 b) { return a.x.CompareTo(b.x); } }
public class RoomFinder
{
    List<Vector3> pointCloud = new List<Vector3>();
    List<int>     hull       = new List<int>();
    List<int>     hullTmp    = new List<int>();

    public Bounds FindBounds()
    {
        Bounds result = new Bounds(pointCloud[0], Vector3.zero);
        for (int i = 0; i<pointCloud.Count; i++){
            result.Encapsulate(pointCloud[i]);
        }
        return result;
    }
    static Vector2 Project(Vector2 a, Vector2 b, Vector2 pt)
    {
        Vector2 ap    = pt - a;
        Vector2 ab    = b - a;
        float   ab2   = ab.x * ab.x + ab.y * ab.y;
        float   ap_ab = ap.x * ab.x + ap.y * ab.y;
        float   t     = ap_ab / ab2;
        return a + ab * t;
    }
    static int CounterClockwise(Vector3 a, Vector3 root, Vector3 b)
    {
        a = a-root;
        b = b-root;
        return a.z * b.x > a.x * b.z ? 1 : -1;
    }
    void Hull()
    {
        // Convex hull Monotone chain
        // https://en.wikibooks.org/wiki/Algorithm_Implementation/Geometry/Convex_hull/Monotone_chain

        // pointCloud is already sorted on the x-axis

        // Go from left to right to calculate the top
        hull   .Clear();
        hullTmp.Clear();
        for (int i = 0; i < pointCloud.Count; i++)
        {
            while (hull.Count > 1 && CounterClockwise(
                pointCloud[hull[hull.Count - 2]],
                pointCloud[hull[hull.Count - 1]],
                pointCloud[i]) < 0)
            {
                hull.RemoveAt(hull.Count-1);
            }
            hull.Add(i);
        }
        hull.RemoveAt(hull.Count-1);

        // Now go from right to left, calculate the bottom
        for (int i = pointCloud.Count-1; i >= 0; i--)
        {
            while (hullTmp.Count > 1 && CounterClockwise(
                pointCloud[hullTmp[hullTmp.Count - 2]],
                pointCloud[hullTmp[hullTmp.Count - 1]],
                pointCloud[i]) < 0)
            {
                hullTmp.RemoveAt(hullTmp.Count - 1);
            }
            hullTmp.Add(i);
        }
        hullTmp.RemoveAt(hullTmp.Count-1);

        // Now combine the top and bottom
        hull.AddRange(hullTmp);
    }
    List<Vector2> Fit()
    {
        // Caliper alg.
        // http://datagenetics.com/blog/march12014/index.html

        int   edge   = -1;
        float size   = float.MaxValue;
        float vSize  = 0;
        float hLeft  = 0;
        float hRight = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            // pick an edge to find a rect for
            Vector3 es1 = pointCloud[hull[i]];
            Vector3 es2 = pointCloud[hull[(i+1)%hull.Count]];
            Vector2 e1 = new Vector2(es1.x, es1.z);
            Vector2 e2 = new Vector2(es2.x, es2.z);
            Vector2 v2 = e2-e1;
            v2 = e1 + new Vector2(v2.y, -v2.x);
            
            // Find the vertical and horizontal extents of the rect, (relative to the edge orientation)
            float height = 0;
            float xmax   = 0;
            float xmin   = 0;
            for (int p = 0; p < hull.Count; p++)
            {
                Vector3 pt3 = pointCloud[hull[p]];
                Vector2 pt  = new Vector2 (pt3.x, pt3.z);
                Vector2 h   = Project(e1, e2, pt); // Project point onto horizontal axis
                Vector2 v   = Project(e1, v2, pt); // project point onto vertical axis
                float hmag = (pt - h).sqrMagnitude;
                float vmag = (pt - v).sqrMagnitude;
                if (height < hmag)
                    height = hmag;
                if (Vector2.Dot(e2-e1, pt-e1) > 0) {
                    if (xmax < vmag)
                        xmax = vmag;
                } else {
                    if (xmin < vmag)
                        xmin = vmag;
                }
            }
            
            // Check if the extents are smaller than the others
            height = Mathf.Sqrt(height);
            xmax   = Mathf.Sqrt(xmax);
            xmin   = Mathf.Sqrt(xmin);
            float currSize = height * (xmax + xmin);
            if (currSize < size)
            {
                edge = i;
                size = currSize;

                vSize  =  height;
                hLeft  = -xmin;
                hRight =  xmax;
            }
        }

        // Calculate the rectangle from the extents
        Vector3 start3 = pointCloud[hull[edge]];
        Vector3 dir3   = pointCloud[hull[(edge + 1) % hull.Count]] - start3;
        Vector2 start = new Vector2(start3.x, start3.z);
        Vector2 dir   = new Vector2(dir3.x, dir3.z).normalized;
        Vector2 down  = new Vector2(-dir.y, dir.x);
        
        List<Vector2> result = new List<Vector2>();
        result.Add(start + dir * hLeft);
        result.Add(start + dir * hRight);
        result.Add(result[1] + down * vSize);
        result.Add(result[0] + down * vSize);
        return result;
    }
    
    public void Add(Vector3 aPt)
    {
        int i = pointCloud.BinarySearch(aPt, new VecCmp());
        if (i<0) i = ~i;
        else
        {
            if ((pointCloud[i]-aPt).sqrMagnitude < 0.00001f)
                return;
        }
        pointCloud.Insert(i, aPt);
    }

    public void DrawGizmos()
    {
        // Preview the convex hull
        Gizmos.color = Color.green;
        Hull();
        if (hull.Count > 1)
        { 
            for (int i = 0; i < hull.Count; i++)
            {
                int next = (i+1)%hull.Count;
                Gizmos.DrawLine(pointCloud[hull[i]], pointCloud[hull[next]]);
            }
        }

        // Show the close fit rectangle
        Gizmos.color = Color.red;
        List<Vector2> corners = Fit();
        for (int i = 0; i < corners.Count; i++)
        {
            int next = (i + 1) % corners.Count;
            Gizmos.DrawLine(new Vector3(corners[i].x, 0, corners[i].y), new Vector3(corners[next].x, 0, corners[next].y));
        }
    }
}
