using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HoldemHand;
using System.Diagnostics;

namespace RiverSolver
{
	class Program
	{
		enum Algorithms { ChanceSampling, ChanceSampling2, ExternalSampling, OutcomeSampling, PublicChanceSampling };

		static bool IsPublicAlgorithm(Algorithms a) { return a >= Algorithms.PublicChanceSampling; }

		static KISSRandom rnd = new KISSRandom();
		const int _47C2 = 1081;

		static void Main(string[] args)
		{
			var board = Hand.ParseHand("5c 9d 3h As Kd");

			var distributions = new HoleDistribution[2];
			distributions[0] = new HoleDistribution();
			distributions[1] = new HoleDistribution();

			int index = 0;

			foreach (var hole in Hand.Hands(0, board, 2).OrderBy(h => Hand.Evaluate(h | board)))
			//foreach (var hole in SimpleDistribution().OrderBy(h => Hand.Evaluate(h | board)))
			{
				var rank = Hand.Evaluate(hole | board);

				/*
				// random distributions
				distributions[0].AddHole(new Hole(index, hole, rank, rnd.NextDouble()));
				distributions[1].AddHole(new Hole(index, hole, rank, rnd.NextDouble()));
				*/

				// uniform distributions
				distributions[0].AddHole(new Hole(index, hole, rank, 1.0));
				distributions[1].AddHole(new Hole(index, hole, rank, 1.0));

				index++;
			}

			distributions[0].SetRelativeProbabilities(distributions[1]);
			distributions[1].SetRelativeProbabilities(distributions[0]);


			Run(distributions, Algorithms.PublicChanceSampling, args.Length > 0 ? long.Parse(args[0]) : 30);

			
		}

		static IEnumerable<ulong> SimpleDistribution()
		{
			yield return Hand.ParseHand("3d 3s");
			yield return Hand.ParseHand("5h 5s");
			yield return Hand.ParseHand("9h 9s");
			//yield return Hand.ParseHand("Kh Ks");
			//yield return Hand.ParseHand("Ad Ah");
		}

		static void Run(HoleDistribution[] distributions, Algorithms algo, long duration, double exploration = 0)
		{
			var game = CreateGame4(distributions);
	
			if (exploration > 0)
				Console.WriteLine("{0} (exploration {1}) {2} seconds", algo, exploration, duration);
			else
				Console.WriteLine("{0} {1} seconds", algo, duration);

			if (IsPublicAlgorithm(algo))
				RunPublic(game, distributions, algo, duration, exploration);
			else
				Run(game, distributions, algo, duration, exploration);

			//game.DumpStrategy(distributions);
		}

		static void Run(Decision game, HoleDistribution[] distributions, Algorithms algo, long duration, double exploration = 0)
		{
			var stopWatch = new Stopwatch();

			int i = 0;

			while (stopWatch.ElapsedMilliseconds < duration * 1000)
			{
				var iteration = new Iteration(distributions);

				stopWatch.Start();
				Train(game, iteration, algo, exploration);
				stopWatch.Stop();

				if (i % 4000000 == 0)
				{
					double es = stopWatch.ElapsedMilliseconds / 1000.0;

					Console.Write("{0} | {1}s | {2} | ", 
						i.ToString().PadLeft(10), 
						es.ToString("F1").PadLeft(10), 
						((i > 0 ? string.Format("{0}", (int)(i / es)) : "i/s").PadLeft(10)));

					var br0 = game.BestResponse(0, distributions);
					Console.Write("BR {0:F4} + ", br0);
					var br1 = game.BestResponse(1, distributions);
					Console.WriteLine("{0:F4} = {1:F4}", br1, br0 + br1);

				}

				i++;
			}

		}

		static void RunPublic(Decision game, HoleDistribution[] distributions, Algorithms algo, long duration, double exploration = 0)
		{
			var stopWatch = new Stopwatch();

			int i = 0;

			while (stopWatch.ElapsedMilliseconds < duration * 1000)
			{
				var iteration = new PublicIteration(distributions);

				stopWatch.Start();
				TrainPublic(game, iteration, algo, exploration);
				stopWatch.Stop();

				if (i % 1000 == 0)
				{
					double es = stopWatch.ElapsedMilliseconds / 1000.0;

					Console.Write("{0} | {1}s | {2} | ",
						i.ToString().PadLeft(10),
						es.ToString("F1").PadLeft(10),
						((i > 0 ? string.Format("{0}", (int)(i / es)) : "i/s").PadLeft(10)));

					var br0 = game.BestResponse(0, distributions);
					Console.Write("BR {0:F4} + ", br0);
					var br1 = game.BestResponse(1, distributions);
					Console.WriteLine("{0:F4} = {1:F4}", br1, br0 + br1);

				}

				i++;
			}

		}

		static void Train(Decision game, Iteration iteration, Algorithms algo, double exploration)
		{
			if (algo == Algorithms.ChanceSampling)
			{
				game.TrainChanceSampling(0, iteration, 1.0, 1.0);
				game.TrainChanceSampling(1, iteration, 1.0, 1.0);
			}
			else if (algo == Algorithms.ChanceSampling2)
			{
				game.TrainChanceSampling2(iteration, new double[] { 1.0, 1.0 });
			}
			else if (algo == Algorithms.ExternalSampling)
			{
				if (rnd.NextDouble() < 0.01)
				{
					game.TrainChanceSampling(0, iteration, 1.0, 1.0);
					game.TrainChanceSampling(1, iteration, 1.0, 1.0);
				}
				else
				{
					game.TrainExternalSampling(0, iteration, 1.0, 1.0);
					game.TrainExternalSampling(1, iteration, 1.0, 1.0);
				}
			}
			else if (algo == Algorithms.OutcomeSampling)
			{
				game.TrainOutcomeSampling(iteration, new double[] { 1.0, 1.0 }, 1.0, exploration);
			}

		}

		static void TrainPublic(Decision game, PublicIteration iteration, Algorithms algo, double exploration)
		{
			if (algo == Algorithms.PublicChanceSampling)
			{
				var p0 = iteration.GetHoles(0).Select(h => h.Probability).ToArray();
				var p1 = iteration.GetHoles(1).Select(h => h.Probability).ToArray();

				game.TrainPublicChanceSampling(0, iteration, p0, p1);
				game.TrainPublicChanceSampling(1, iteration, p1, p0);
			}
		}


		static Decision CreateGame(HoleDistribution[] d)
		{
			return
				new Decision
				(
					0, d,
					/*
					new Decision
					(
						1, d,
						new Showdown(3),
						new Fold(1)
					),
					 */
					new Decision
					(
						1, d,
						new Showdown(2),
						new Fold(1)
					),
					new Decision
					(
						1, d,
						/*
						new Decision
						(
							0, d,
							new Showdown(3),
							new Fold(-1)
						),
						 */
						new Decision
						(
							0, d,
							new Showdown(2),
							new Fold(-1)
						),
						new Showdown(1)
					)

				);
		}

		static Decision CreateGame4(HoleDistribution[] d)
		{
			return
				new Decision
				(
					0, d,
					new Decision
					(
						1, d,
						new Decision
						(
							0, d,
							new Showdown(3),
							new Fold(-2)
						),
						new Showdown(2),
						new Fold(1)
					),
					new Decision
					(
						1, d,
						new Decision
						(
							0, d,
							new Showdown(6),
							new Fold(-3)
						),
						new Showdown(3),
						new Fold(1)
					),
					new Decision
					(
						1, d,
						new Decision
						(
							0, d,
							new Decision
							(
								1, d,
								new Showdown(3),
								new Fold(2)
							),
							new Showdown(2),
							new Fold(-1)
						),
						new Decision
						(
							0, d,
							new Decision
							(
								1, d,
								new Showdown(6),
								new Fold(3)
							),
							new Showdown(3),
							new Fold(-1)
						),
						new Showdown(1)
					)

				);
		}
	

	}
}
