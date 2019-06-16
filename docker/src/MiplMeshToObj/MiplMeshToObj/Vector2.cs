using System;


namespace MiplMeshToObj
{
	public struct Vector2
	{
		public const double kEpsilon = 1E-05d;
		public double x;
		public double y;

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
					default:
						throw new IndexOutOfRangeException("Invalid Vector2d index!");
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
					default:
						throw new IndexOutOfRangeException("Invalid Vector2d index!");
				}
			}
		}


		public double magnitude
		{
			get
			{
				return Math.Sqrt(this.x * this.x + this.y * this.y);
			}
		}

		public double sqrMagnitude
		{
			get
			{
				return this.x * this.x + this.y * this.y;
			}
		}


		public Vector2(double x, double y)
		{
			this.x = x;
			this.y = y;
		}


		public static Vector2 operator +(Vector2 a, Vector2 b)
		{
			return new Vector2(a.x + b.x, a.y + b.y);
		}

		public static Vector2 operator -(Vector2 a, Vector2 b)
		{
			return new Vector2(a.x - b.x, a.y - b.y);
		}

		public static Vector2 operator -(Vector2 a)
		{
			return new Vector2(-a.x, -a.y);
		}

		public static Vector2 operator *(Vector2 a, double d)
		{
			return new Vector2(a.x * d, a.y * d);
		}

		public static Vector2 operator *(double d, Vector2 a)
		{
			return new Vector2(a.x * d, a.y * d);
		}

		public static Vector2 operator *(float d, Vector2 a)
		{
			return new Vector2(a.x * d, a.y * d);
		}

		public static Vector2 operator /(Vector2 a, double d)
		{
			return new Vector2(a.x / d, a.y / d);
		}

		public static bool operator ==(Vector2 lhs, Vector2 rhs)
		{
			return Vector2.SqrMagnitude(lhs - rhs) < 0.0 / 1.0;
		}

		public static bool operator !=(Vector2 lhs, Vector2 rhs)
		{
			return (double)Vector2.SqrMagnitude(lhs - rhs) >= 0.0 / 1.0;
		}

		public static Vector2 zero
		{
			get
			{
				return new Vector2(0.0d, 0.0d);
			}
		}

		public static Vector2 one
		{
			get
			{
				return new Vector2(1d, 1d);
			}
		}

		public void Normalize()
		{
			double magnitude = this.magnitude;
			if (magnitude > 9.99999974737875E-06)
				this = this / magnitude;
			else
				this = Vector2.zero;
		}

		public override string ToString()
		{
			return String.Format("( {0:0.00000}, {1:0.00000} )", x, y);
		}

		public string ToString(string format)
		{
			return String.Format("[ {0} {1} ]", x.ToString(format), y.ToString(format));
		}

		public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2;
		}

		public override bool Equals(object other)
		{
			if (!(other is Vector2))
				return false;
			Vector2 vector2d = (Vector2)other;
			if (this.x.Equals(vector2d.x))
				return this.y.Equals(vector2d.y);
			else
				return false;
		}


		public static double SqrMagnitude(Vector2 a)
		{
			return (a.x * a.x + a.y * a.y);
		}

	}
}
