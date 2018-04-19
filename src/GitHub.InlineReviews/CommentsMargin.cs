﻿using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Reactive.Linq;
using GitHub.Models;
using GitHub.Commands;
using GitHub.Services;
using GitHub.Extensions;
using GitHub.InlineReviews.Views;
using GitHub.InlineReviews.ViewModels;
using GitHub.InlineReviews.Commands;
using Microsoft.VisualStudio.Text.Editor;
using ReactiveUI;
using Task = System.Threading.Tasks.Task;

namespace GitHub.InlineReviews
{
    internal class CommentsMargin : IWpfTextViewMargin
    {
        public const string MarginName = "CommentsMargin";

        readonly IWpfTextView textView;
        readonly CommentsMarginViewModel viewModel;
        readonly CommentsMarginView visualElement;
        readonly IPullRequestSessionManager sessionManager;

        bool isDisposed;

        IDisposable currentSessionSubscription;
        IDisposable optionChangedSubscription;
        IDisposable visibilitySubscription;

        public CommentsMargin(
            IWpfTextView textView,
            IEnableInlineCommentsCommand enableInlineCommentsCommand,
            INextInlineCommentCommand nextInlineCommentCommand,
            IPullRequestSessionManager sessionManager)
        {
            this.textView = textView;
            this.sessionManager = sessionManager;

            viewModel = new CommentsMarginViewModel(enableInlineCommentsCommand, nextInlineCommentCommand);
            visualElement = new CommentsMarginView { DataContext = viewModel, ClipToBounds = true };

            visibilitySubscription = viewModel.WhenAnyValue(x => x.Enabled).Subscribe(enabled =>
            {
                visualElement.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            });

            optionChangedSubscription = Observable.FromEventPattern(textView.Options, nameof(textView.Options.OptionChanged)).Subscribe(_ =>
            {
                viewModel.MarginEnabled = textView.Options.GetOptionValue<bool>(InlineCommentMarginEnabled.OptionName);
            });

            currentSessionSubscription = sessionManager.WhenAnyValue(x => x.CurrentSession)
                .Subscribe(x => RefreshCurrentSession().Forget());
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                GC.SuppressFinalize(this);
                isDisposed = true;

                currentSessionSubscription.Dispose();
                optionChangedSubscription.Dispose();
                visibilitySubscription.Dispose();
            }
        }

        async Task RefreshCurrentSession()
        {
            var sessionFile = await FindSessionFile();
            if (sessionFile != null)
            {
                viewModel.FileName = Path.GetFileName(sessionFile.RelativePath);
                viewModel.CommentsInFile = sessionFile.InlineCommentThreads?.Count ?? -1;
                viewModel.Enabled = sessionFile.Diff.Count > 0;
            }
            else
            {
                viewModel.CommentsInFile = 0;
                viewModel.Enabled = false;
            }
        }

        async Task<IPullRequestSessionFile> FindSessionFile()
        {
            await sessionManager.EnsureInitialized();

            var session = sessionManager.CurrentSession;
            if (session == null)
            {
                return null;
            }

            var relativePath = sessionManager.GetRelativePath(textView.TextBuffer);
            if (relativePath == null)
            {
                return null;
            }

            return await session.GetFile(relativePath);
        }

        public FrameworkElement VisualElement => visualElement;

        public double MarginSize => visualElement.ActualHeight;

        public bool Enabled => viewModel.Enabled;

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }
    }
}