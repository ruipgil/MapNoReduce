using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace PADIMapNoReduce
{
	public class Async
	{
		public static Thread ExecInThread(Action exec) {
			ThreadStart ts = new ThreadStart(exec);
			Thread t = new Thread(ts);
			t.Start();
			return t;
		}

		/// <summary>
		/// Executes a function at each element of a list.
		/// Order NOT guaranteed.
		/// Blocks to wait for all
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static List<Thread> eachBlocking<T>(List<T> list, Action<T> each, int max) {
			var t = each<T> (list, each, max);
			t.ForEach (x => x.Join ());
			return t;
		}
		public static List<Thread> eachBlocking<T>(List<T> list, Action<T> each) {
			var t = each<T> (list, each, list.Count);
			t.ForEach (x => x.Join ());
			return t;
		}
		/// <summary>
		/// Similar to threadEachBlocking, but doesn't block the flow.
		/// </summary>
		/// <returns>The each.</returns>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static List<Thread> each<T>(List<T> list, Action<T> each, int max) {
			if (max > list.Count) {
				max = list.Count;
			}

			return list.GetRange(0, max).Select (x=>{
				Thread t = new Thread(()=>{
					//try {
						each(x);
					//}catch(Exception e) {
					//	Console.WriteLine("Error at each!");
					//	Console.WriteLine(e);
					//}
				});
				t.Start();
				return t;
			}).ToList();
		}
		public static List<Thread> each<T>(List<T> list, Action<T> each) {
			return each<T> (list, each, list.Count);
		}

		/// <summary>
		/// Same as eachLImit, but blocking.
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <param name="limit">Limit.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void eachLimitBlocking<T>(List<T> list, Action<T> each, int limit) {
			Parallel.ForEach (list,  new ParallelOptions { MaxDegreeOfParallelism = limit }, each);
		}

		/// <summary>
		/// Runs a forEach parallel, that has a limit of how many parallel tasks there are.
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <param name="limit">Limit.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void eachLimit<T>(List<T> list, Action<T> each, int limit) {
			ExecInThread(()=>{
				Parallel.ForEach (list,  new ParallelOptions { MaxDegreeOfParallelism = limit }, each);
			});
		}

		/*public delegate List<T> ListCaller<T>();
		public static void eachLimitBlockingDynamic<T>(ListCaller<T> listCaller, Action<T> each, int limit) {
			var list = listCaller ();
			var threads = new List<Thread> ();
			for (var i = 0; i < limit; i++) {
				new Thread ();
			}
		}*/

		/*public void workDistribution(Action canProceed, List<string> workers, Action effective, int limit) {
			limit = Math.Min (limit, workers.Count);
			int i = 0;
			int current = 0;
			Action exec;
			Action next = () => {
				i++; // lock
				current++; // lock
				var th = new Thread(exec);

			};
			exec = () => {
				
				effective (workers [i]);
				if (canProceed () && current < limit) {
					next();
				}
			};
			Thread t = new Thread(()=>{
				
			});
		}*/
	}
}

