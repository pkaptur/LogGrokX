#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using LogGrokX.Data;
using LogGrokX.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DisplaySettingsMonitorTests
{
    [TestMethod]
    public void BackgroundSettingsChangesAreDeliveredOnOwnerThread()
    {
        RunOnSta(() =>
        {
            var previous = BinaryDetectionOptions.Current;
            try
            {
                BinaryDetectionOptions.Current = BinaryDetectionOptions.All;
                var dispatcher = Dispatcher.CurrentDispatcher;
                using var monitor = new DisplaySettingsMonitor(new WpfUiDispatcher(dispatcher));
                var part = new LinePartViewModel(1, "SGVsbG8sIHdvcmxkIQ==", monitor);
                var threads = new List<int>();
                ((INotifyPropertyChanged)part).PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);
                var owner = Environment.CurrentManagedThreadId;

                Task.Run(() =>
                {
                    Assert.IsNull(Dispatcher.FromThread(Thread.CurrentThread));
                    BinaryDetectionOptions.Current = BinaryDetectionOptions.None;
                    Assert.IsNull(Dispatcher.FromThread(Thread.CurrentThread));
                }).GetAwaiter().GetResult();

                Assert.AreEqual(0, threads.Count);
                dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                Assert.IsGreaterThan(0, threads.Count);
                Assert.IsTrue(threads.TrueForAll(thread => thread == owner));
                Assert.IsFalse(part.IsBase64);
            }
            finally { BinaryDetectionOptions.Current = previous; }
        });
    }

    [TestMethod]
    public void DisposalCancelsQueuedNotifications()
    {
        RunOnSta(() =>
        {
            var previous = BinaryDetectionOptions.Current;
            try
            {
                BinaryDetectionOptions.Current = BinaryDetectionOptions.All;
                var dispatcher = Dispatcher.CurrentDispatcher;
                using var monitor = new DisplaySettingsMonitor(new WpfUiDispatcher(dispatcher));
                var part = new LinePartViewModel(1, "SGVsbG8sIHdvcmxkIQ==", monitor);
                var changes = 0;
                part.PropertyChanged += (_, _) => changes++;
                Task.Run(() => BinaryDetectionOptions.Current = BinaryDetectionOptions.None).GetAwaiter().GetResult();
                monitor.Dispose();
                dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

                Assert.AreEqual(0, changes);
                GC.KeepAlive(part);
            }
            finally { BinaryDetectionOptions.Current = previous; }
        });
    }

    [TestMethod]
    public void MonitorDoesNotKeepDiscardedCellsAlive()
    {
        using var monitor = new DisplaySettingsMonitor(TestServices.Inline);
        var cell = CreateWeakCell(monitor);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.IsFalse(cell.IsAlive);
        GC.KeepAlive(monitor);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateWeakCell(DisplaySettingsMonitor monitor) =>
        new(new LinePartViewModel(1, "value", monitor));

    private static void RunOnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception e) { error = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
