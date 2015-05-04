using System;
using System.Threading;
using System.Collections.Generic;

namespace PADIMapNoReduce
{
	public class Utils
	{
		public delegate void ExecMethod();
		public static Thread ExecInThread(ExecMethod exec) {
			ThreadStart ts = new ThreadStart(exec);
			Thread t = new Thread(ts);
			t.Start();
			return t;
		}

		public delegate void Each<T>(T elm);
		/// <summary>
		/// Executes a function at each element of a list.
		/// Order NOT guaranteed.
		/// Blocks to wait for all
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void threadEachBlocking<T>(List<T> list, Each<T> each, int max) {
			threadEach (list, each, max).Join();
		}
		public static void threadEachBlocking<T>(List<T> list, Each<T> each) {
			threadEach (list, each, list.Count).Join();
		}
		/// <summary>
		/// Similar to threadEachBlocking, but doesn't block the flow.
		/// </summary>
		/// <returns>The each.</returns>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static Thread threadEach<T>(List<T> list, Each<T> each, int max) {
			ThreadStart ts = new ThreadStart(()=>{});
			int count = list.Count;
			for (var i=0; i<count; i++) {
				var elm = list [i];
				ts += (()=>{
					each(elm);
				});
			}
			Thread t = new Thread (ts);
			t.Start ();
			return t;
		}
		public static Thread threadEach<T>(List<T> list, Each<T> each) {
			return threadEach (list, each, list.Count);
		}
	}
}

