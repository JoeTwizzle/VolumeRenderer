using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Code
{
    /// <summary>
    /// Defines an axis-aligned 3d box (rectangular prism).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct Box3i : IEquatable<Box3i>
    {
        /// <summary>
        /// An empty box with Min (0, 0, 0) and Max (0, 0, 0).
        /// </summary>
        public static readonly Box3i Empty = new Box3i(0, 0, 0, 0, 0, 0);

        private Vector3Int _min;

        /// <summary>
        /// Gets or sets the minimum boundary of the structure.
        /// </summary>
        public Vector3Int Min
        {
            get => _min;
            set
            {
                _max = Vector3Int.Max(_max, value);
                _min = value;
            }
        }

        private Vector3Int _max;

        /// <summary>
        /// Gets or sets the maximum boundary of the structure.
        /// </summary>
        public Vector3Int Max
        {
            get => _max;
            set
            {
                _min = Vector3Int.Min(_min, value);
                _max = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3i"/> struct.
        /// </summary>
        /// <param name="min">The minimum point in 3D space this box encloses.</param>
        /// <param name="max">The maximum point in 3D space this box encloses.</param>
        public Box3i(Vector3Int min, Vector3Int max)
        {
            _min = Vector3Int.Min(min, max);
            _max = Vector3Int.Max(min, max);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3i"/> struct.
        /// </summary>
        /// <param name="minX">The minimum x value to be enclosed.</param>
        /// <param name="minY">The minimum y value to be enclosed.</param>
        /// <param name="minZ">The minimum z value to be enclosed.</param>
        /// <param name="maxX">The maximum x value to be enclosed.</param>
        /// <param name="maxY">The maximum y value to be enclosed.</param>
        /// <param name="maxZ">The maximum z value to be enclosed.</param>
        public Box3i(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
            : this(new Vector3Int(minX, minY, minZ), new Vector3Int(maxX, maxY, maxZ))
        {
        }

        /// <summary>
        /// Gets a vector describing the size of the Box3i structure.
        /// </summary>
        [XmlIgnore]
        public Vector3Int Size
        {
            get => Max - Min;
        }

        public int Volume
        {
            get
            {
                var size = Size;
                return size.x * size.y * size.z;
            }
        }

        public long LongVolume
        {
            get
            {
                var size = Size;
                return (long)size.x * size.y * size.z;
            }
        }

        /// <summary>
        /// Gets or sets a vector describing half the size of the box.
        /// </summary>
        [XmlIgnore]
        public Vector3Int HalfSize
        {
            get => Size / 2;
            set
            {
                Vector3Int center = new Vector3Int((int)Center.x, (int)Center.y, (int)Center.z);
                _min = center - value;
                _max = center + value;
            }
        }

        /// <summary>
        /// Gets a vector describing the center of the box.
        /// </summary>
        /// to avoid annoying off-by-one errors in box placement, no setter is provided for this property
        [XmlIgnore]
        public Vector3 Center
        {
            get => _min + ((Vector3)(_max - _min) * 0.5f);
        }

        /// <summary>
        /// Returns whether the box contains the specified point (borders inclusive).
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <returns>Whether this box contains the point.</returns>

        public readonly bool Contains(Vector3Int point)
        {
            return _min.x <= point.x && point.x < _max.x &&
                   _min.y <= point.y && point.y < _max.y &&
                   _min.z <= point.z && point.z < _max.z;
        }

        /// <summary>
        /// Returns whether the box contains the specified point (borders inclusive).
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <returns>Whether this box contains the point.</returns>

        public readonly bool ContainsInclusive(Vector3Int point)
        {
            return _min.x <= point.x && point.x <= _max.x &&
                   _min.y <= point.y && point.y <= _max.y &&
                   _min.z <= point.z && point.z <= _max.z;
        }

        /// <summary>
        /// Returns whether the box contains the specified point (borders exclusive).
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <returns>Whether this box contains the point.</returns>

        public readonly bool ContainsExclusive(Vector3Int point)
        {
            return _min.x < point.x && point.x < _max.x &&
                   _min.y < point.y && point.y < _max.y &&
                   _min.z < point.z && point.z < _max.z;
        }

        /// <summary>
        /// Returns whether the box contains the specified point.
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <param name="boundaryInclusive">
        /// Whether points on the box boundary should be recognised as contained as well.
        /// </param>
        /// <returns>Whether this box contains the point.</returns>

        public bool Contains(Vector3Int point, bool boundaryInclusive)
        {
            if (boundaryInclusive)
            {
                return ContainsInclusive(point);
            }
            else
            {
                return ContainsExclusive(point);
            }
        }
        /// <summary>
        /// Returns whether the box overlaps the specified box (borders inclusive).
        /// </summary>
        /// <param name="other">The box to query.</param>
        /// <returns>Whether this box overlaps the other box.</returns>

        public bool Overlaps(Box3i other)
        {
            return Min.x <= other.Min.x && Max.x >= other.Max.x &&
               Min.y <= other.Min.y && Max.y >= other.Max.y &&
               Min.z <= other.Min.z && Max.z >= other.Max.z;
        }
        /// <summary>
        /// Returns whether the box contains the specified box (borders inclusive).
        /// </summary>
        /// <param name="other">The box to query.</param>
        /// <returns>Whether this box contains the other box.</returns>

        public bool Contains(Box3i other)
        {
            return _max.x >= other._min.x && _min.x <= other._max.x &&
                   _max.y >= other._min.y && _min.y <= other._max.y &&
                   _max.z >= other._min.z && _min.z <= other._max.z;
        }

        /// <summary>
        /// Returns the distance between the nearest edge and the specified point.
        /// </summary>
        /// <param name="point">The point to find distance for.</param>
        /// <returns>The distance between the specified point and the nearest edge.</returns>

        public float DistanceToNearestEdge(Vector3Int point)
        {
            var dist = new Vector3(
                Math.Max(0f, Math.Max(_min.x - point.x, point.x - _max.x)),
                Math.Max(0f, Math.Max(_min.y - point.y, point.y - _max.y)),
                Math.Max(0f, Math.Max(_min.z - point.z, point.z - _max.z)));
            return dist.magnitude;
        }

        /// <summary>
        /// Translates this Box3i by the given amount.
        /// </summary>
        /// <param name="distance">The distance to translate the box.</param>
        public void Translate(Vector3Int distance)
        {
            _min += distance;
            _max += distance;
        }

        /// <summary>
        /// Returns a Box3i translated by the given amount.
        /// </summary>
        /// <param name="distance">The distance to translate the box.</param>
        /// <returns>The translated box.</returns>

        public Box3i Translated(Vector3Int distance)
        {
            // create a local copy of this box
            Box3i box = this;
            box.Translate(distance);
            return box;
        }

        /// <summary>
        /// Scales this Box3i by the given amount.
        /// </summary>
        /// <param name="scale">The scale to scale the box.</param>
        /// <param name="anchor">The anchor to scale the box from.</param>
        public void Scale(Vector3Int scale, Vector3Int anchor)
        {
            _min = anchor + ((_min - anchor) * scale);
            _max = anchor + ((_max - anchor) * scale);
        }

        /// <summary>
        /// Returns a Box3i scaled by a given amount from an anchor point.
        /// </summary>
        /// <param name="scale">The scale to scale the box.</param>
        /// <param name="anchor">The anchor to scale the box from.</param>
        /// <returns>The scaled box.</returns>

        public Box3i Scaled(Vector3Int scale, Vector3Int anchor)
        {
            // create a local copy of this box
            Box3i box = this;
            box.Scale(scale, anchor);
            return box;
        }

        /// <summary>
        /// Inflate this Box3i to encapsulate a given point.
        /// </summary>
        /// <param name="point">The point to query.</param>
        [Obsolete("Use " + nameof(Extend) + " instead. This function will have it's implementation changed in the future.")]
        public void Inflate(Vector3Int point)
        {
            _min = Vector3Int.Min(_min, point);
            _max = Vector3Int.Max(_max, point);
        }

        /// <summary>
        /// Inflate this Box3i to encapsulate a given point.
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <returns>The inflated box.</returns>

        [Obsolete("Use " + nameof(Extended) + " instead. This function will have it's implementation changed in the future.")]
        public Box3i Inflated(Vector3Int point)
        {
            // create a local copy of this box
            Box3i box = this;
            box.Inflate(point);
            return box;
        }

        /// <summary>
        /// Extend this Box3i to encapsulate a given point.
        /// </summary>
        /// <param name="point">The point to contain.</param>
        public void Extend(Vector3Int point)
        {
            _min = Vector3Int.Max(_min, point);
            _max = Vector3Int.Max(_max, point);
        }

        /// <summary>
        /// Extend this Box3i to encapsulate a given point.
        /// </summary>
        /// <param name="point">The point to contain.</param>
        /// <returns>The inflated box.</returns>

        public Box3i Extended(Vector3Int point)
        {
            // create a local copy of this box
            Box3i box = this;
            box.Extend(point);
            return box;
        }

        /// <summary>
        /// Equality comparator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        public static bool operator ==(Box3i left, Box3i right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality comparator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        public static bool operator !=(Box3i left, Box3i right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Box3i && Equals((Box3i)obj);
        }

        /// <inheritdoc/>
        public bool Equals(Box3i other)
        {
            return _min.Equals(other._min) &&
                   _max.Equals(other._max);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(_min, _max);
        }
    }
}
