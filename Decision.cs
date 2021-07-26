using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RiverSolver
{
	class Decision : GameTreeNode
	{
		const double Epsilon = 1e-7;

		static KISSRandom rnd = new KISSRandom();

		int player;
		double[,] regret;
		double[,] cumulativeStrategy;

		public Decision(int player, HoleDistribution[] distributions, params GameTreeNode[] c)
		{
			this.player = player;
			children = c.ToArray();
			regret = new double[distributions[player].HoleCount, c.Length];
			cumulativeStrategy = new double[distributions[player].HoleCount, c.Length];
		}

		int SampleStrategy(double[] s)
		{
			double r = rnd.NextDouble();

			double acc = 0;

			for (int i = 0; i < children.Length; i++)
			{
				acc += s[i];
				if (r < acc) return i;
			}

			throw new Exception();
		}
		 
		public override double TrainChanceSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			if (p < Epsilon && op < Epsilon) return 0.0;
			//if (op < Epsilon) return 0.0;

			int hole = iteration.GetHandIndex(player);

			var s = GetStrategy(hole);

			if (player == trainplayer)
			{
				for (int i = 0; i < children.Length; i++)
					cumulativeStrategy[hole, i] += p * s[i];

				var u = new double[children.Length];

				double ev = 0;

				for (int i = 0; i < children.Length; i++)
				{
					u[i] = children[i].TrainChanceSampling(trainplayer, iteration, p * s[i], op);
					ev += u[i] * s[i];
				}

				for (int i = 0; i < children.Length; i++)
					regret[hole, i] += u[i] - ev;

				return ev;

			}
			else
			{
				double ev = 0;

				for (int i = 0; i < children.Length; i++)
					ev += children[i].TrainChanceSampling(trainplayer, iteration, p, op * s[i]);

				return ev;

			}
		}

		public override double[] TrainPublicChanceSampling(int trainplayer, PublicIteration iteration, double[] p, double[] op)
		{
			var s = new double[p.Length][];

			for (int i = 0; i < p.Length; i++)
				s[i] = GetStrategy(i);

			if (player == trainplayer)
			{

				for (int i = 0; i < p.Length; i++)
				{
					for (int j = 0; j < children.Length; j++)
						cumulativeStrategy[i, j] += p[i] * s[i][j];
				}

				var u = new double[children.Length][];
				var ev = new double[p.Length];

				for (int j = 0; j < children.Length; j++)
				{
					var newp = new double[p.Length];
					for (int i = 0; i < p.Length; i++)
						newp[i] = s[i][j] * p[i];

					u[j] = children[j].TrainPublicChanceSampling(trainplayer, iteration, newp, op);

					for (int i = 0; i < p.Length; i++)
						ev[i] += u[j][i] * s[i][j];
				}


				for (int i = 0; i < p.Length; i++)
				{
					for (int j = 0; j < children.Length; j++)
						regret[i, j] += u[j][i] - ev[i];
				}

				return ev;

			}
			else
			{
				var ev = new double[p.Length];

				for (int j = 0; j < children.Length; j++)
				{
					var newop = new double[p.Length];
					for (int i = 0; i < p.Length; i++)
						newop[i] = s[i][j] * op[i];

					var u = children[j].TrainPublicChanceSampling(trainplayer, iteration, p, newop);

					for (int i = 0; i < p.Length; i++)
						ev[i] += u[i];
				}

				return ev;

			}
		}

		public override double[] TrainChanceSampling2(Iteration iteration, double[] reach)
		{
			if (reach[0] < Epsilon && reach[1] < Epsilon) return new double[] { 0.0, 0.0 };

			int hole = iteration.GetHandIndex(player);

			var s = GetStrategy(hole);

			for (int i = 0; i < children.Length; i++)
				cumulativeStrategy[hole, i] += reach[player] * s[i];

			var u = new double[children.Length][];
			var ev = new double[2];

			for (int i = 0; i < children.Length; i++)
			{
				var newreach = new double[2];
				newreach[player] = reach[player] * s[i];
				newreach[player ^ 1] = reach[player ^ 1];
				u[i] = children[i].TrainChanceSampling2(iteration, newreach);
				ev[player] += u[i][player] * s[i];
				ev[player ^ 1] += u[i][player ^ 1];
			}

			for (int i = 0; i < children.Length; i++)
				regret[hole, i] += u[i][player] - ev[player];

			return ev;
		}

		public override double TrainExternalSampling(int trainplayer, Iteration iteration, double p, double op)
		{
			int hole = iteration.GetHandIndex(player);

			var s = GetStrategy(hole);

			if (player == trainplayer)
			{
				for (int i = 0; i < children.Length; i++)
					cumulativeStrategy[hole, i] += (1.0 / op) * p * s[i];

				var u = new double[children.Length];

				double ev = 0;

				for (int i = 0; i < children.Length; i++)
				{
					u[i] = children[i].TrainExternalSampling(trainplayer, iteration, p * s[i], op);
					ev += u[i] * s[i];
				}

				for (int i = 0; i < children.Length; i++)
					regret[hole, i] += u[i] - ev;

				return ev;

			}
			else
			{
				int a = SampleStrategy(s);
				return children[a].TrainExternalSampling(trainplayer, iteration, p, op * s[a]);
			}
		}

		public override double[] TrainOutcomeSampling(Iteration iteration, double[] reach, double sp, double exploration)
		{
			int hole = iteration.GetHandIndex(player);

			var s = GetStrategy(hole);

			for (int i = 0; i < children.Length; i++)
				cumulativeStrategy[hole, i] += reach[player] * s[i] / sp;

			int sampledAction = rnd.NextDouble() < exploration ? rnd.Next(children.Length) : SampleStrategy(s);

			reach[player] *= s[sampledAction];
			var csp = exploration * (1.0 / children.Length) + (1 - exploration) * s[sampledAction];
			var ev = children[sampledAction].TrainOutcomeSampling(iteration, reach, sp * csp, exploration);

			regret[hole, sampledAction] += ev[player];
			ev[player] *= s[sampledAction];
			for (int i = 0; i < children.Length; i++) regret[hole, i] -= ev[player];

			return ev;
		}

		double[] GetStrategy(int hand)
		{
			var s = new double[children.Length];

			double psum = 0;

			for (int i = 0; i < children.Length; i++)
				if (regret[hand, i] > 0) 
					psum += regret[hand, i];

			if (psum > 0)
			{
				for (int i = 0; i < children.Length; i++)
					s[i] = (regret[hand, i] > 0) ? regret[hand, i] / psum : 0.0;
			}
			else
			{
				for (int i = 0; i < children.Length; i++)
					s[i] = 1.0 / children.Length;
			}

			return s;
		}

		double[] GetNormalizedAverageStrategy(int hand)
		{
			var s = new double[children.Length];
			
			double sum = 0;
			for (int i = 0; i < children.Length; i++) sum += cumulativeStrategy[hand, i];
			if (sum > 0)
				for (int i = 0; i < children.Length; i++) s[i] = cumulativeStrategy[hand, i] / sum;
			else
				for (int i = 0; i < children.Length; i++) s[i] = 1.0 / children.Length;

			return s;

		}

		public double BestResponse(int brplayer, HoleDistribution[] distributions)
		{
			int bropponent = brplayer ^ 1;

			var op = new double[distributions[bropponent].HoleCount];
			double sum = 0;

			for (int i = 0; i < distributions[brplayer].HoleCount; i++)
			{
				var phole = distributions[brplayer].Holes[i];

				double opsum = 0;

				for (int j = 0; j < distributions[bropponent].HoleCount; j++)
				{
					var ohole = distributions[bropponent].Holes[j];
					op[j] = phole.Overlaps(ohole) ? 0.0 : ohole.Probability;
					opsum += op[j];
				}

				for (int j = 0; j < distributions[bropponent].HoleCount; j++)
					op[j] /= opsum;

				sum += phole.RelativeProbability * BestResponse(brplayer, distributions, i, op);
			}

			return sum;
		}

		public override double BestResponse(int brplayer, HoleDistribution[] distributions, int hand, double[] op)
		{
			int bropponent = brplayer ^ 1;

			if (player == brplayer)
			{
				double bestev = -double.MaxValue;

				for (int i = 0; i < children.Length; i++)
					bestev = Math.Max(bestev, children[i].BestResponse(brplayer, distributions, hand, op));

				return bestev;
			}
			else
			{
				double ev = 0;

				for (int i = 0; i < children.Length; i++)
				{
					var newop = new double[distributions[bropponent].HoleCount];

					for (int h = 0; h < distributions[bropponent].HoleCount; h++)
					{
						var s = GetNormalizedAverageStrategy(h);
						newop[h] = s[i] * op[h];
					}

					ev += children[i].BestResponse(brplayer, distributions, hand, newop);
				}

				return ev;
			}
		}

		public void DumpStrategy(HoleDistribution[] distributions)
		{
			Console.WriteLine("Player {0}:", player);

			for (int h = 0; h < distributions[player].HoleCount; h++)
			{
				var s = GetNormalizedAverageStrategy(h);
				Console.Write("{0} ", HoldemHand.Hand.MaskToString(distributions[player].Holes[h].CardMask));

				for (int i = 0; i < children.Length; i++)
					Console.Write("{0} ", Math.Round(s[i] * 100.0));

				Console.Write("| ");

				for (int i = 0; i < children.Length; i++)
					Console.Write("{0} ", cumulativeStrategy[h, i]);

				Console.Write("| ");

				for (int i = 0; i < children.Length; i++)
					Console.Write("{0} ", regret[h, i]);

				Console.WriteLine();

			}

			for (int i = 0; i < children.Length; i++)
			{
				if (children[i] is Decision)
					(children[i] as Decision).DumpStrategy(distributions);
			}
		}
	}
}
