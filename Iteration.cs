using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class Iteration
	{
		Hole[] holes = new Hole[2];

		public Iteration(HoleDistribution[] distributions)
		{
			do
			{
				holes[0] = distributions[0].Sample();
				holes[1] = distributions[1].Sample();
			} while (holes[0].Overlaps(holes[1]));

		}

		public int GetHandIndex(int player)
		{
			return holes[player].Index;
		}

		public double GetShowdownValue(int player)
		{
			if (holes[player].Rank > holes[player ^ 1].Rank)
				return 1.0;
			else if (holes[player].Rank < holes[player ^ 1].Rank)
				return -1.0;
			else
				return 0.0;

		}
	}
}
