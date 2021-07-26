using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class PublicIteration
	{
		HoleDistribution[] distributions;

		public PublicIteration(HoleDistribution[] distributions)
		{
			this.distributions = distributions;
		}

		public List<Hole> GetHoles(int player)
		{
			return distributions[player].Holes;
		}

	}
}
