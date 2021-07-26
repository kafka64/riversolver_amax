using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class HoleDistribution
	{
		static KISSRandom rnd = new KISSRandom();
		List<Hole> holes = new List<Hole>();

		public int HoleCount { get { return holes.Count; } }
		public List<Hole> Holes { get { return holes; } }

		public void AddHole(Hole h)
		{
			holes.Add(h);
		}

		public void SetRelativeProbabilities(HoleDistribution ohd)
		{
			double totalrelp = 0;

			for (int i = 0; i < holes.Count; i++)
			{
				double op = 0;

				for (int j = 0; j < ohd.holes.Count; j++)
					if (!holes[i].Overlaps(ohd.holes[j]))
						op += ohd.holes[j].Probability;

				holes[i].RelativeProbability = op * holes[i].Probability;
				totalrelp += holes[i].RelativeProbability;
			}

			for (int i = 0; i < holes.Count; i++)
				holes[i].RelativeProbability /= totalrelp;

		}

		public Hole Sample()
		{
			while (true)
			{
				int i = (int)(rnd.RandomDoubleLeftClosed() * holes.Count);
				if (holes[i].Probability == 1.0 || rnd.RandomDoubleLeftClosed() < holes[i].Probability) return holes[i];
			}
		}


	}
}
