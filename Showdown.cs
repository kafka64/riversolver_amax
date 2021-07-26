using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RiverSolver
{
	class Showdown : GameTreeNode
	{
		double value;

		public Showdown(double value)
		{
			this.value = value;
		}

		public override double TrainChanceSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			return iteration.GetShowdownValue(trainplayer) * op * value;
		}

		public override double[] TrainChanceSampling2(Iteration iteration, double[] reach)
		{
			return new double[] 
			{ 
				iteration.GetShowdownValue(0) * reach[1] * value,
				iteration.GetShowdownValue(1) * reach[0] * value
			};

		}

		public override double TrainExternalSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			return iteration.GetShowdownValue(trainplayer) * value;
		}

		public override double[] TrainOutcomeSampling(Iteration iteration, double[] reach, double sp, double exploration)
		{
			return new double[] 
			{ 
				iteration.GetShowdownValue(0) * reach[1] * value / sp,
				iteration.GetShowdownValue(1) * reach[0] * value / sp
			};

		}

		public double[] TrainPublicChanceSamplingSlow(int trainplayer, PublicIteration iteration, double[] p, double[] op)
		{
			var pholes = iteration.GetHoles(trainplayer);
			var oholes = iteration.GetHoles(trainplayer ^ 1);

			var ev = new double[p.Length];

			for (int i = 0; i < p.Length; i++)
			{
				double sum = 0;

				for (int j = 0; j < op.Length; j++)
				{
					if (!pholes[i].Overlaps(oholes[j]))
					{
						if (pholes[i].Rank > oholes[j].Rank)
							sum += op[j];
						else if (pholes[i].Rank < oholes[j].Rank)
							sum -= op[j];
					}
				}

				ev[i] = sum * value;
			}

			return ev;
		}

		public override double[] TrainPublicChanceSampling(int trainplayer, PublicIteration iteration, double[] p, double[] op)
		{
			var pholes = iteration.GetHoles(trainplayer);
			var oholes = iteration.GetHoles(trainplayer ^ 1);

			var ev = new double[p.Length];

			var wincr = new double[52];

			double winsum = 0;
			int j = 0;

			for (int i = 0; i < p.Length; i++)
			{
				while (oholes[j].Rank < pholes[i].Rank)
				{
					winsum += op[j];
					wincr[oholes[j].Card1] += op[j];
					wincr[oholes[j].Card2] += op[j];
					j++;
				}

				ev[i] = (winsum - wincr[pholes[i].Card1] - wincr[pholes[i].Card2]) * value;
			}

			var losecr = new double[52];
			double losesum = 0;
			j = op.Length - 1;

			for (int i = p.Length - 1; i >= 0; i--)
			{
				while (oholes[j].Rank > pholes[i].Rank)
				{
					losesum += op[j];
					losecr[oholes[j].Card1] += op[j];
					losecr[oholes[j].Card2] += op[j];
					j--;
				}

				ev[i] -= (losesum - losecr[pholes[i].Card1] - losecr[pholes[i].Card2]) * value;
			}

#if DEBUG
			// verify the correctness of our algorithm
			var sev = TrainPublicChanceSamplingSlow(trainplayer, iteration, p, op);

			for (int i = 0; i < ev.Length; i++)
			{
				if (Math.Abs(ev[i] - sev[i]) > 0.0000001)
					throw new Exception(string.Format("fail {0}", Math.Abs(ev[i] - sev[i])));
			}

#endif
			return ev;
		}

		public override double BestResponse(int brplayer, HoleDistribution[] distributions, int i, double[] op)
		{
			double ev = 0;
			for (int j = 0; j < op.Length; j++)
			{
				var prank = distributions[brplayer].Holes[i].Rank;
				var orank = distributions[brplayer ^ 1].Holes[j].Rank;

				if (prank > orank)
					ev += value * op[j];
				else if (prank < orank)
					ev -= value * op[j];
			}

			return ev;
		}

	}
}
