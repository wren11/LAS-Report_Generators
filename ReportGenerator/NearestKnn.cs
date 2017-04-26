using System;
using System.Collections.Generic;
using Point3d = Atlass.Core.TcReportPoint3D;
using Point3dCollection = System.Collections.ObjectModel.Collection
    <Atlass.Core.TcReportPoint3D>;

namespace PointKdTree
{

    /// <summary>
    /// Node of a Point3dTree
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Creates a new instance of TreeNode
        /// </summary>
        /// <param name="value">The 3d point value of the node.</param>
        public TreeNode(Point3d value) { this.Value = value; }

        /// <summary>
        /// Gets the value (Point3d) of the node.
        /// </summary>
        public Point3d Value { get; internal set; }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        public TreeNode Parent { get; internal set; }

        /// <summary>
        /// Gets the left child node.
        /// </summary>
        public TreeNode LeftChild { get; internal set; }

        /// <summary>
        /// Gets the right child node.
        /// </summary>
        public TreeNode RightChild { get; internal set; }

        /// <summary>
        /// Gets the depth of the node in tree.
        /// </summary>
        public int Depth { get; internal set; }

        /// <summary>
        /// Gets a value indicating if the current node is a LeftChild node.
        /// </summary>
        public bool IsLeft { get; internal set; }
    }

    /// <summary>
    /// Provides methods to organize 3d points in a Kd tree structure to speed up the search of neighbours.
    /// A boolean constructor parameter (ignoreZ) indicates if the resulting Kd tree is a 3d tree or a 2d tree.
    /// Use ignoreZ = true if all points in the input collection lie on a plane parallel to XY
    /// or if the points have to be considered as projected on the XY plane.
    /// </summary>
    public class Point3dTree
    {
        #region Private fields

        private int dimension;
        private int parallelDepth;
        private bool ignoreZ;
        private Func<Point3d, Point3d, double> sqrDist;

        public bool Ready = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an new instance of Point3dTree.
        /// </summary>
        /// <param name="points">The Point3d collection to fill the tree.</param>
        /// <param name="ignoreZ">A value indicating if the Z coordinate of points is ignored
        /// (as if all points were projected to the XY plane).</param>
        public Point3dTree(ref Point3d[] points, bool ignoreZ = false)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            this.ignoreZ = ignoreZ;
            this.dimension = ignoreZ ? 2 : 3;
            if (ignoreZ)
                this.sqrDist = SqrDistance2d;
            else
                this.sqrDist = SqrDistance3d;
            int numProc = System.Environment.ProcessorCount;
            this.parallelDepth = -1;
            while (numProc >> ++this.parallelDepth > 1) ;
            
            this.Root = Create(ref points, 0, null, false);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        public TreeNode Root { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the nearest neighbour.
        /// </summary>
        /// <param name="point">The point from which search the nearest neighbour.</param>
        /// <returns>The nearest point in the collection from the specified one.</returns>
        public Point3d NearestNeighbour(Point3d point)
        {
            return GetNeighbour(point, this.Root, this.Root.Value, double.MaxValue);
        }

        /// <summary>
        /// Gets the neighbours within the specified distance.
        /// </summary>
        /// <param name="point">The point from which search the nearest neighbours.</param>
        /// <param name="radius">The distance in which collect the neighbours.</param>
        /// <returns>The points which distance from the specified point is less or equal to the specified distance.</returns>
        public Point3dCollection NearestNeighbours(Point3d point, double radius)
        {
            Point3dCollection points = new Point3dCollection();
            GetNeighboursAtDistance(point, radius * radius, this.Root, points);
            return points;
        }

        /// <summary>
        /// Gets the given number of nearest neighbours.
        /// </summary>
        /// <param name="point">The point from which search the nearest neighbours.</param>
        /// <param name="number">The number of points to collect.</param>
        /// <returns>The n nearest neighbours of the specified point.</returns>
        public List<Point3d> NearestNeighbours(Point3d point, int number)
        {
            List<Tuple<double, Point3d>> pairs = new List<Tuple<double, Point3d>>(number);
            GetKNeighbours(point, number, this.Root, pairs);
            var points = new List<Point3d>();
            for (int i = 0; i < pairs.Count; i++)
            {
                points.Add(pairs[i].Item2);
            }
            return points;
        }

        /// <summary>
        /// Gets the points in a range.
        /// </summary>
        /// <param name="pt1">The first corner of range.</param>
        /// <param name="pt2">The opposite corner of the range.</param>
        /// <returns>All points within the box.</returns>
        public Point3dCollection BoxedRange(Point3d pt1, Point3d pt2)
        {
            Point3d lowerLeft = new Point3d(
                Math.Min(pt1.X, pt2.X), Math.Min(pt1.Y, pt2.Y), Math.Min(pt1.Z, pt2.Z));
            Point3d upperRight = new Point3d(
                Math.Max(pt1.X, pt2.X), Math.Max(pt1.Y, pt2.Y), Math.Max(pt1.Z, pt2.Z));
            Point3dCollection points = new Point3dCollection();
            FindRange(lowerLeft, upperRight, this.Root, points);
            return points;
        }

        /// <summary>
        /// Gets all the pairs of points which distance is less or equal than the specified distance.
        /// </summary>
        /// <param name="radius">The maximum distance between two points. </param>
        /// <returns>The pairs of points which distance is less or equal than the specified distance.</returns>
        public List<Tuple<Point3d, Point3d>> ConnectAll(double radius)
        {
            List<Tuple<Point3d, Point3d>> connexions = new List<Tuple<Point3d, Point3d>>();
            GetConnexions(this.Root, radius * radius, connexions);
            return connexions;
        }

        #endregion

        public EventHandler Updater = null;

        #region Private methods

        public TreeNode Create(ref Point3d[] points, int depth, TreeNode parent, bool isLeft)
        {

            Updater?.Invoke(depth, new EventArgs());

            int length = points.Length;
            if (length == 0) return null;
            int d = depth % this.dimension;
            Point3d median = points.QuickSelectMedian((p1, p2) => p1[d].CompareTo(p2[d]));
            TreeNode node = new TreeNode(median);
            node.Depth = depth;
            node.Parent = parent;
            node.IsLeft = isLeft;
            int mid = length / 2;
            int rlen = length - mid - 1;
            Point3d[] left = new Point3d[mid];
            Point3d[] right = new Point3d[rlen];
            Array.Copy(points, 0, left, 0, mid);
            Array.Copy(points, mid + 1, right, 0, rlen);
            if (depth < this.parallelDepth)
            {
                System.Threading.Tasks.Parallel.Invoke(
                   () => node.LeftChild = Create(ref left, depth + 1, node, true),
                   () => node.RightChild = Create(ref right, depth + 1, node, false)
                );
            }
            else
            {
                node.LeftChild = Create(ref left, depth + 1, node, true);
                node.RightChild = Create(ref right, depth + 1, node, false);
            }

            Ready = true;

            return node;
        }

        private Point3d GetNeighbour(Point3d center, TreeNode node, Point3d currentBest, double bestDist)
        {
            if (node == null)
                return currentBest;
            Point3d current = node.Value;
            int d = node.Depth % this.dimension;
            double coordCen = center[d];
            double coordCur = current[d];
            double dist = this.sqrDist(center, current);
            if (dist >= 0.0 && dist < bestDist)
            {
                currentBest = current;
                bestDist = dist;
            }
            dist = coordCen - coordCur;
            if (bestDist < dist * dist)
            {
                currentBest = GetNeighbour(
                    center, coordCen < coordCur ? node.LeftChild : node.RightChild, currentBest, bestDist);
                bestDist = this.sqrDist(center, currentBest);
            }
            else
            {
                currentBest = GetNeighbour(center, node.LeftChild, currentBest, bestDist);
                bestDist = this.sqrDist(center, currentBest);
                currentBest = GetNeighbour(center, node.RightChild, currentBest, bestDist);
                bestDist = this.sqrDist(center, currentBest);
            }
            return currentBest;
        }

        private void GetNeighboursAtDistance(Point3d center, double radius, TreeNode node, Point3dCollection points)
        {
            if (node == null) return;
            Point3d current = node.Value;
            double dist = this.sqrDist(center, current);
            if (dist <= radius)
            {
                points.Add(current);
            }
            int d = node.Depth % this.dimension;
            double coordCen = center[d];
            double coordCur = current[d];
            dist = coordCen - coordCur;
            if (dist * dist > radius)
            {
                if (coordCen < coordCur)
                {
                    GetNeighboursAtDistance(center, radius, node.LeftChild, points);
                }
                else
                {
                    GetNeighboursAtDistance(center, radius, node.RightChild, points);
                }
            }
            else
            {
                GetNeighboursAtDistance(center, radius, node.LeftChild, points);
                GetNeighboursAtDistance(center, radius, node.RightChild, points);
            }
        }

        private void GetKNeighbours(Point3d center, int number, TreeNode node, List<Tuple<double, Point3d>> pairs)
        {
            if (node == null) return;
            Point3d current = node.Value;
            double dist = this.sqrDist(center, current);
            int cnt = pairs.Count;
            if (cnt == 0)
            {
                pairs.Add(new Tuple<double, Point3d>(dist, current));
            }
            else if (cnt < number)
            {
                if (dist > pairs[0].Item1)
                {
                    pairs.Insert(0, new Tuple<double, Point3d>(dist, current));
                }
                else
                {
                    pairs.Add(new Tuple<double, Point3d>(dist, current));
                }
            }
            else if (dist < pairs[0].Item1)
            {
                pairs[0] = new Tuple<double, Point3d>(dist, current);
                pairs.Sort((p1, p2) => p2.Item1.CompareTo(p1.Item1));
            }
            int d = node.Depth % this.dimension;
            double coordCen = center[d];
            double coordCur = current[d];
            dist = coordCen - coordCur;
            if (dist * dist > pairs[0].Item1)
            {
                if (coordCen < coordCur)
                {
                    GetKNeighbours(center, number, node.LeftChild, pairs);
                }
                else
                {
                    GetKNeighbours(center, number, node.RightChild, pairs);
                }
            }
            else
            {
                GetKNeighbours(center, number, node.LeftChild, pairs);
                GetKNeighbours(center, number, node.RightChild, pairs);
            }
        }

        private void FindRange(Point3d lowerLeft, Point3d upperRight, TreeNode node, Point3dCollection points)
        {
            if (node == null)
                return;
            Point3d current = node.Value;
            if (ignoreZ)
            {
                if (current.X >= lowerLeft.X && current.X <= upperRight.X &&
                    current.Y >= lowerLeft.Y && current.Y <= upperRight.Y)
                    points.Add(current);
            }
            else
            {
                if (current.X >= lowerLeft.X && current.X <= upperRight.X &&
                    current.Y >= lowerLeft.Y && current.Y <= upperRight.Y &&
                    current.Z >= lowerLeft.Z && current.Z <= upperRight.Z)
                    points.Add(current);
            }
            int d = node.Depth % this.dimension;
            if (upperRight[d] < current[d])
                FindRange(lowerLeft, upperRight, node.LeftChild, points);
            else if (lowerLeft[d] > current[d])
                FindRange(lowerLeft, upperRight, node.RightChild, points);
            else
            {
                FindRange(lowerLeft, upperRight, node.LeftChild, points);
                FindRange(lowerLeft, upperRight, node.RightChild, points);
            }
        }

        private void GetConnexions(TreeNode node, double radius, List<Tuple<Point3d, Point3d>> connexions)
        {
            if (node == null) return;
            Point3dCollection points = new Point3dCollection();
            Point3d center = node.Value;
            if (ignoreZ)
                GetRightParentsNeighbours(center, node, radius, points);
            GetNeighboursAtDistance(center, radius, node.LeftChild, points);
            GetNeighboursAtDistance(center, radius, node.RightChild, points);
            for (int i = 0; i < points.Count; i++)
            {
                connexions.Add(new Tuple<Point3d, Point3d>(center, points[i]));
            }
            GetConnexions(node.LeftChild, radius, connexions);
            GetConnexions(node.RightChild, radius, connexions);
        }

        private void GetRightParentsNeighbours(Point3d center, TreeNode node, double radius, Point3dCollection points)
        {
            TreeNode parent = GetRightParent(node);
            if (parent == null) return;
            int d = parent.Depth % this.dimension;
            double dist = center[d] - parent.Value[d];
            if (dist * dist <= radius)
            {
                GetNeighboursAtDistance(center, radius, parent.RightChild, points);
            }
            GetRightParentsNeighbours(center, parent, radius, points);
        }

        private TreeNode GetRightParent(TreeNode node)
        {
            TreeNode parent = node.Parent;
            if (parent == null) return null;
            if (node.IsLeft) return parent;
            return GetRightParent(parent);
        }

        private double SqrDistance2d(Point3d p1, Point3d p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) +
                (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }

        private double SqrDistance3d(Point3d p1, Point3d p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) +
                (p1.Y - p2.Y) * (p1.Y - p2.Y) +
                (p1.Z - p2.Z) * (p1.Z - p2.Z);
        }

        #endregion
    }

    static class Extensions
    {
        // Credit: Tony Tanzillo
        // http://www.theswamp.org/index.php?topic=44312.msg495808#msg495808
        public static T QuickSelectMedian<T>(this T[] items, Comparison<T> compare)
        {
            int l = items.Length;
            int k = l / 2;
            if (items == null || l == 0)
                throw new ArgumentException("array");
            int from = 0;
            int to = l - 1;
            while (from < to)
            {
                int r = from;
                int w = to;
                T current = items[(r + w) / 2];
                while (r < w)
                {
                    if (compare(items[r], current) > -1)
                    {
                        var tmp = items[w];
                        items[w] = items[r];
                        items[r] = tmp;
                        w--;
                    }
                    else
                    {
                        r++;
                    }
                }
                if (compare(items[r], current) > 0)
                {
                    r--;
                }
                if (k <= r)
                {
                    to = r;
                }
                else
                {
                    from = r + 1;
                }
            }
            return items[k];
        }
    }
}