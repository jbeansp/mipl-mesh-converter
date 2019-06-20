using System;
using System.Collections.Generic;
using System.Text;

namespace MiplMeshToObj
{
	class Vector4
	{
		public double x;
		public double y;
		public double z;
		public double w;

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
					case 3:
						return this.w;
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
					case 3:
						this.w = value;
						break;
					default:
						throw new IndexOutOfRangeException("Invalid Vector4 index");
				}
			}
		}

		public double magnitude
		{
			get
			{
				return Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w);
			}
		}

		public double sqrMagnitude
		{
			get
			{
				return this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w;
			}
		}

		public static Vector4 zero
		{
			get
			{
				return new Vector4(0d, 0d, 0d, 0d);
			}
		}

		public static Vector4 one
		{
			get
			{
				return new Vector4(1d, 1d, 1d, 1d);
			}
		}

		public Vector4(double x, double y, double z, double w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public Vector4(float x, float y, float z, float w)
		{
			this.x = (double)x;
			this.y = (double)y;
			this.z = (double)z;
			this.w = (double)w;
		}


		public static Vector4 operator +(Vector4 a, Vector4 b)
		{
			return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
		}

		public static Vector4 operator -(Vector4 a, Vector4 b)
		{
			return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
		}

		public static Vector4 operator -(Vector4 a)
		{
			return new Vector4(-a.x, -a.y, -a.z, -a.w);
		}

		public static Vector4 operator *(Vector4 a, double d)
		{
			return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
		}

		public static Vector4 operator *(double d, Vector4 a)
		{
			return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
		}

		public static Vector4 operator /(Vector4 a, double d)
		{
			return new Vector4(a.x / d, a.y / d, a.z / d, a.w / d);
		}

		public static bool operator ==(Vector4 lhs, Vector4 rhs)
		{
			return (double)Vector4.SqrMagnitude(lhs - rhs) < 0.0 / 1.0;
		}

		public static bool operator !=(Vector4 lhs, Vector4 rhs)
		{
			return (double)Vector4.SqrMagnitude(lhs - rhs) >= 0.0 / 1.0;
		}

		public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2 ^ this.w.GetHashCode() << 2;
		}

		public override bool Equals(object other)
		{
			if (!(other is Vector4))
				return false;
			Vector4 Vector4 = (Vector4)other;
			if (this.x.Equals(Vector4.x) && this.y.Equals(Vector4.y))
				return this.z.Equals(Vector4.z);
			else
				return false;
		}

		public static double SqrMagnitude(Vector4 a)
		{
			return a.x * a.x + a.y * a.y + a.z * a.z + a.w * a.w;
		}

		public override string ToString()
		{
			return String.Format("( {0:0.00000}, {1:0.00000}, {2:0.00000}, {3:0.00000} )", x, y, z, w);
		}

	}
}
