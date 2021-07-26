using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class Hole
	{
		public int Index;
		public ulong CardMask;
		public uint Rank;
		public double Probability;
		public double RelativeProbability;
		public byte Card1;
		public byte Card2;
			
		public Hole(int index, ulong cardmask, uint rank, double probability)
		{
			Index = index;
			CardMask = cardmask;
			Rank = rank;
			Probability = probability;
			Card1 = (byte)Util.GetLowestBitIndex(cardmask);
			Card2 = (byte)Util.GetHighestBitIndex(cardmask);
		}

		public bool Overlaps(Hole x)
		{
			return (CardMask & x.CardMask) != 0;
		}

	}
}
