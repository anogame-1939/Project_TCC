// AnoGame.Application/Utils/Vector3Extensions.cs
using UnityEngine;
using AnoGame.Domain.Data.Models;

namespace AnoGame.Application.Utils
{
    public static class Vector3Extensions
    {
        public static Vector3 ToVector3(this Position3D position)
        {
            return new Vector3(position.X, position.Y, position.Z);
        }

        public static Position3D ToPosition3D(this Vector3 vector)
        {
            return new Position3D(vector.x, vector.y, vector.z);
        }

        public static Quaternion ToQuaternion(this Rotation3D rotation)
        {
            return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static Rotation3D ToRotation3D(this Quaternion quaternion)
        {
            return new Rotation3D(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static Vector3? ToVector3(this Position3D? position)
        {
            return position.HasValue ? position.Value.ToVector3() : null;
        }
    }
}