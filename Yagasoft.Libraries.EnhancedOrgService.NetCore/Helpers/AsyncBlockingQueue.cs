using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Microsoft.Identity.Client;
using System.Collections;

namespace Yagasoft.Libraries.EnhancedOrgService.NetCore.Helpers
{
	public class AsyncBlockingQueue<T> : BlockingCollection<T>
	{
		// Synchronization primitives.
		private readonly AsyncLock mutex;
		private readonly AsyncConditionVariable notFull;
		private readonly AsyncConditionVariable notEmpty;

		// Convenience properties to make the code a bit clearer.
		private bool Empty => Count == 0;
		private bool Full => Count == BoundedCapacity;
		
		#region ctor(s)

		public AsyncBlockingQueue() : this(new ConcurrentQueue<T>())
		{ }

		public AsyncBlockingQueue(int maxSize) : this(new ConcurrentQueue<T>(), maxSize)
		{ }

		public AsyncBlockingQueue(ConcurrentQueue<T> queue, int maxSize = int.MaxValue) : base(new ConcurrentQueue<T>(), maxSize)
		{
			mutex = new AsyncLock();
			notFull = new AsyncConditionVariable(mutex);
			notEmpty = new AsyncConditionVariable(mutex);
		}

		#endregion ctor(s)

		#region Methods

		/// <summary>
		///     Enqueue an Item
		/// </summary>
		/// <param name="item">Item to enqueue</param>
		/// <remarks>Blocks if the blocking queue is full</remarks>
		public async Task Enqueue(T item)
		{
			using (await mutex.LockAsync())
			{
				while (Full)
				{
					await notFull.WaitAsync();
				}

				Add(item);
				notEmpty.Notify();
			}
		}

		/// <summary>
		///     Dequeue an item
		/// </summary>
		/// <param name="timeout">[Optional] The number of milliseconds to timeout while waiting. Value of -1 is infinite wait.</param>
		/// <returns>Item dequeued</returns>
		/// <remarks>Blocks if the blocking queue is empty</remarks>
		/// <exception cref="TimeoutException">Timeout expired.</exception>
		public async Task<T> Dequeue(TimeSpan timeout)
		{
			using (await mutex.LockAsync())
			{
				while (Empty)
				{
					await notEmpty.WaitAsync();
				}

				try
				{
					var ret = Take(new CancellationTokenSource(timeout).Token);
					notFull.Notify();
					return ret;
				}
				catch (OperationCanceledException)
				{
					throw new TimeoutException($"Timeout while waiting for an item to be added to the queue (timeout: {timeout} ms).");
				}
			}
		}

		public async Task<T> Dequeue(CancellationTokenSource? cancellationToken = default)
		{
			using (await mutex.LockAsync())
			{
				while (Empty)
				{
					await notEmpty.WaitAsync();
				}

				if (cancellationToken == default)
				{
					var ret = Take();
					notFull.Notify();
					return ret;
				}

				try
				{
					var ret = Take(cancellationToken.Token);
					notFull.Notify();
					return ret;
				}
				catch (OperationCanceledException)
				{
					return default;
				}
			}
		}

		/// <summary>
		///     Clears the queue of all items
		/// </summary>
		public async Task Clear()
		{
			while (this.Any())
			{
				await Dequeue();
			}
		}

		#endregion Methods
	}
}
