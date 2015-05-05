﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace PADIMapNoReduce
{
	public class Async
	{
		public delegate void ExecMethod();
		public static Thread ExecInThread(ExecMethod exec) {
			ThreadStart ts = new ThreadStart(exec);
			Thread t = new Thread(ts);
			t.Start();
			return t;
		}

		public delegate void EachFn<T>(T elm);
		/// <summary>
		/// Executes a function at each element of a list.
		/// Order NOT guaranteed.
		/// Blocks to wait for all
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void eachBlocking<T>(List<T> list, EachFn<T> each, int max) {
			each<T> (list, each, max).ForEach (x => x.Join ());
		}
		public static void eachBlocking<T>(List<T> list, EachFn<T> each) {
			each<T> (list, each, list.Count).ForEach (x => x.Join ());
		}
		/// <summary>
		/// Similar to threadEachBlocking, but doesn't block the flow.
		/// </summary>
		/// <returns>The each.</returns>
		/// <param name="list">List.</param>
		/// <param name="each">Each.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static List<Thread> each<T>(List<T> list, EachFn<T> each, int max) {
			if (max > list.Count) {
				max = list.Count;
			}
			return list.GetRange(0, max).Select (x=>{
				Thread t = new Thread(()=>each(x));
				t.Start();
				return t;
			}).ToList();
		}
		public static List<Thread> each<T>(List<T> list, EachFn<T> each) {
			return each<T> (list, each, list.Count);
		}
	}
}

