using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiverSolver
{
	abstract class GameTreeNode
	{
		protected GameTreeNode[] children;

		public GameTreeNode[] Children { get { return children; } }

		public abstract double TrainChanceSampling(int trainplayer, Iteration iteration, double p, double op);
		public abstract double[] TrainChanceSampling2(Iteration iteration, double[] reach);
		public abstract double TrainExternalSampling(int trainplayer, Iteration iteration, double p, double op);
		public abstract double[] TrainOutcomeSampling(Iteration iteration, double[] reach, double sp, double exploration);

		public abstract double[] TrainPublicChanceSampling(int trainplayer, PublicIteration iteration, double[] p, double[] op);

		public abstract double BestResponse(int brplayer, HoleDistribution[] distributions, int hand, double[] op);

	}
}
