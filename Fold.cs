using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	class Fold : GameTreeNode
	{
		double value;

		public Fold(double value)
		{
			this.value = value;
		}

		public override double TrainChanceSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			if (trainplayer == 0)
				return op * value;
			else
				return op * -value;
		}

		public override double[] TrainChanceSampling2(Iteration iteration, double[] reach)
		{
			return new double[] 
			{ 
				reach[1] * value, 
				reach[0] * -value
			};

		}

		public override double TrainExternalSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			if (trainplayer == 0)
				return value;
			else
				return -value;
		}

		public override double[] TrainOutcomeSampling(Iteration iteration, double[] reach, double sp, double exploration)
		{
			return new double[] 
			{ 
				reach[1] * value / sp, 
				reach[0] * -value / sp
			};

		}

		public double[] TrainPublicChanceSamplingSlow(int trainplayer, PublicIteration iteration, double[] p, double[] op)
		{
			var pholes = iteration.GetHoles(trainplayer);
			var oholes = iteration.GetHoles(trainplayer ^ 1);

			var pvalue = trainplayer == 0 ? value : -value;

			var ev = new double[p.Length];

			for (int i = 0; i < p.Length; i++)
			{
				double sum = 0;

				for (int j = 0; j < op.Length; j++)
				{
					if (!pholes[i].Overlaps(oholes[j]))
						sum += op[j];
				}

				ev[i] = sum * pvalue;
			}

			return ev;
		}

		public override double[] TrainPublicChanceSampling(int trainplayer, PublicIteration iteration, double[] p, double[] op)
		{
			var pholes = iteration.GetHoles(trainplayer);
			var oholes = iteration.GetHoles(trainplayer ^ 1);

			var pvalue = trainplayer == 0 ? value : -value;

			var ev = new double[p.Length];

			double opsum = 0;

			var cr = new double[52];
			for (int j = 0; j < oholes.Count; j++)
			{
				cr[oholes[j].Card1] += op[j];
				cr[oholes[j].Card2] += op[j];
				opsum += op[j];
			}

			for (int i = 0; i < pholes.Count; i++)
			{
				ev[i] = (opsum - cr[pholes[i].Card1] - cr[pholes[i].Card2] + op[i]) * pvalue;	// NOTE: "+ op[i]" works only if the two distributions have the same cards
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

		public override double BestResponse(int brplayer, HoleDistribution[] distributions, int hand, double[] op)
		{
			double ev = 0;
			for (int i = 0; i < op.Length; i++)
				ev += value * op[i];

			return brplayer == 0 ? ev : -ev;
		}


	}
}
