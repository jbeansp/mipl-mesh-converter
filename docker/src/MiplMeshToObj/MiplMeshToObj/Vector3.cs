using System;


namespace MiplMeshToObj
{
	public struct Vector3
	{
		public const float kEpsilon = 1E-05f;
		public double x;
		public double y;
		public double z;

		public double this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return this.x;
					case 1:
						return this.y;
					case 2:
						return this.z;
					default:
						throw new IndexOutOfRangeException("Invalid index");
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						this.x = value;
						break;
					case 1:
						this.y = value;
						break;
					case 2:
						this.z = value;
						break;
					default:
						throw new IndexOutOfRangeException("Invalid Vector3 index");
				}
			}
		}

		public double magnitude
		{
			get
			{
				return Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
			}
		}

		public double sqrMagnitude
		{
			get
			{
				return this.x * this.x + this.y * this.y + this.z * this.z;
			}
		}

		public static Vector3 zero
		{
			get
			{
				return new Vector3(0d, 0d, 0d);
			}
		}

		public static Vector3 one
		{
			get
			{
				return new Vector3(1d, 1d, 1d);
			}
		}

		public Vector3(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Vector3(float x, float y, float z)
		{
			this.x = (double)x;
			this.y = (double)y;
			this.z = (double)z;
		}


		public Vector3(double x, double y)
		{
			this.x = x;
			this.y = y;
			this.z = 0d;
		}

		public static Vector3 operator +(Vector3 a, Vector3 b)
		{
			return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
		}

		public static Vector3 operator -(Vector3 a, Vector3 b)
		{
			return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
		}

		public static Vector3 operator -(Vector3 a)
		{
			return new Vector3(-a.x, -a.y, -a.z);
		}

		public static Vector3 operator *(Vector3 a, double d)
		{
			return new Vector3(a.x * d, a.y * d, a.z * d);
		}

		public static Vector3 operator *(double d, Vector3 a)
		{
			return new Vector3(a.x * d, a.y * d, a.z * d);
		}

		public static Vector3 operator /(Vector3 a, double d)
		{
			return new Vector3(a.x / d, a.y / d, a.z / d);
		}

		public static bool operator ==(Vector3 lhs, Vector3 rhs)
		{
			return (double)Vector3.SqrMagnitude(lhs - rhs) < 0.0 / 1.0;
		}

		public static bool operator !=(Vector3 lhs, Vector3 rhs)
		{
			return (double)Vector3.SqrMagnitude(lhs - rhs) >= 0.0 / 1.0;
		}

		public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
		}

		public override bool Equals(object other)
		{
			if (!(other is Vector3))
				return false;
			Vector3 vector3d = (Vector3)other;
			if (this.x.Equals(vector3d.x) && this.y.Equals(vector3d.y))
				return this.z.Equals(vector3d.z);
			else
				return false;
		}

		public static double SqrMagnitude(Vector3 a)
		{
			return a.x * a.x + a.y * a.y + a.z * a.z;
		}

		public override string ToString()
		{
			//return "(" + this.x + " - " + this.y + " - " + this.z + ")";
			return String.Format("( {0:0.00000}, {1:0.00000}, {2:0.00000} )", x, y, z);
		}

	}
}
