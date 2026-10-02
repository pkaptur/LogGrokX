#nullable enable

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using LogGrokX.MarkedLines;
using LogGrokX.Search;
using LogGrokX.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Tests;

[TestClass]
public class MainWindowViewModelLifecycleTests
{
    [TestMethod]
    public void AddDocument_ExistingPathActivatesOpenDocument()
    {
        RunOnSta(() =>
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"loggrokx-{Guid.NewGuid():N}.log");
            File.WriteAllText(fileName, "2026-01-01 00:00:00 INFO 1 Test Message");
            try
            {
                using var viewModel = CreateViewModel();
                viewModel.AddDocument(fileName);
                var first = viewModel.CurrentDocument;
                Assert.IsNotNull(first);

                var alternatePath = Path.Combine(Path.GetDirectoryName(fileName)!, ".", Path.GetFileName(fileName));
                viewModel.AddDocument(alternatePath);

                Assert.AreEqual(1, viewModel.Documents.Count);
                Assert.AreSame(first, viewModel.CurrentDocument);
            }
            finally
            {
                File.Delete(fileName);
            }
        });
    }

    [TestMethod]
    public void Dispose_DocumentsCollectionDoesNotKeepMarkedLinesViewModelAlive()
    {
        RunOnSta(() =>
        {
            var documents = new ObservableCollection<DocumentViewModel>();
            var reference = CreateAndDisposeMarkedLinesViewModel(documents);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(reference.IsAlive);
            GC.KeepAlive(documents);
        });
    }

    [TestMethod]
    public void RemovingDocument_DocumentDoesNotKeepMarkedLinesViewModelAlive()
    {
        RunOnSta(() =>
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"loggrokx-{Guid.NewGuid():N}.log");
            File.WriteAllText(fileName, "2026-01-01 00:00:00 INFO 1 Test Message");
            try
            {
                var (reference, document) = CreateAndRemoveDocument(fileName);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Assert.IsFalse(reference.IsAlive);
                GC.KeepAlive(document);
            }
            finally
            {
                File.Delete(fileName);
            }
        });
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var settings = ApplicationSettings.Instance();
        return new MainWindowViewModel(
            settings,
            new SearchAutocompleteCache(),
            new SavedSearchPatternStore(),
            new UiThemeService(),
            new TimelinePlacementService(settings),
            new TextZoomService(settings),
            new ThreadGroupingService(settings),
            new MergedFilesViewService(settings),
            new UpdateCheckService(settings),
            documents => new MarkedLinesViewModel(documents));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeMarkedLinesViewModel(
        ObservableCollection<DocumentViewModel> documents)
    {
        var viewModel = new MarkedLinesViewModel(documents);
        viewModel.Dispose();
        return new WeakReference(viewModel);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference reference, DocumentViewModel document) CreateAndRemoveDocument(string fileName)
    {
        using var viewModel = CreateViewModel();
        viewModel.AddDocument(fileName);
        var document = viewModel.CurrentDocument!;
        var reference = new WeakReference(viewModel.MarkedLinesViewModel);
        viewModel.Documents.Remove(document);
        return (reference, document);
    }

    private static void RunOnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null)
            throw error;
    }
}
