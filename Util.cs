using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class Util
	{
		public static int GetLowestBitIndex(ulong x)
		{
			for (int i = 0; i < 64; i++)
			{
				if ((x & (1UL << i)) != 0)
					return i;
			}

			return -1;
		}

		public static int GetHighestBitIndex(ulong x)
		{
			for (int i = 63; i >= 0; i--)
			{
				if ((x & (1UL << i)) != 0)
					return i;
			}

			return -1;
		}

	}
}
