/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Launcher.App.Resources;
using Launcher.App.Services;
using Launcher.App.ViewModels.Multiplayer;
using Launcher.Application.Services;
using Launcher.Application.Services.Multiplayer;

namespace Launcher.Tests.ViewModels.Multiplayer;

public sealed class MultiplayerPageRedstoneTests
{
    [Fact]
    public void TransportPillKeepsTerracottaPositionWhenSwitchingBack()
    {
        var viewModel = Create(new FakeRedstoneService());

        viewModel.SelectTerracottaPageCommand.Execute(MultiplayerPageSection.JoinLobby);
        Assert.True(viewModel.IsJoinLobbySection);

        viewModel.SelectTransportCommand.Execute(MultiplayerTransportKind.Redstone);
        Assert.True(viewModel.IsRedstoneSection);
        Assert.False(viewModel.IsTerracottaSection);

        viewModel.SelectTransportCommand.Execute(MultiplayerTransportKind.Terracotta);
        Assert.True(viewModel.IsJoinLobbySection);
    }

    [Fact]
    public void SelectingRedstoneSectionLoadsNodesAndSelectsFirst()
    {
        var service = new FakeRedstoneService();
        var viewModel = Create(service);

        SelectRedstone(viewModel);

        Assert.True(viewModel.IsRedstoneSection);
        Assert.False(viewModel.IsTerracottaSection);
        Assert.Equal(2, viewModel.RedstoneNodes.Count);
        Assert.Equal("nj.hongshi.site", viewModel.SelectedRedstoneNode?.Address);
        Assert.Equal(1, service.GetNodesCount);
        Assert.True(viewModel.StartRedstoneCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartTunnelPublishesAddressAndStopsOnRequest()
    {
        var service = new FakeRedstoneService();
        var viewModel = Create(service);
        SelectRedstone(viewModel);

        await viewModel.StartRedstoneCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsRedstoneRunning);
        Assert.True(viewModel.HasRedstoneSession);
        Assert.Equal("nj.hongshi.site:44812", viewModel.RedstoneAddress);
        Assert.Equal(25565, service.LastGamePort);
        Assert.False(viewModel.IsRedstoneEditable);
        Assert.True(viewModel.StopRedstoneCommand.CanExecute(null));

        await viewModel.StopRedstoneCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsRedstoneRunning);
        Assert.False(viewModel.HasRedstoneSession);
        Assert.Equal(string.Empty, viewModel.RedstoneAddress);
        Assert.Equal(1, service.StopCount);
    }

    [Fact]
    public async Task InvalidPortBlocksStartAndReportsFailure()
    {
        var service = new FakeRedstoneService { StartFailure = new IOException("boom") };
        var viewModel = Create(service);
        SelectRedstone(viewModel);

        viewModel.RedstoneGamePort = "70000";
        Assert.False(viewModel.StartRedstoneCommand.CanExecute(null));

        viewModel.RedstoneGamePort = "25565";
        await viewModel.StartRedstoneCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsRedstoneRunning);
        Assert.Equal(Strings.Redstone_StartFailed, viewModel.RedstoneStatus);
        Assert.True(viewModel.HasRedstoneStatus);
    }

    [Fact]
    public void NodeFailureShowsStatusAndKeepsSectionUsable()
    {
        var service = new FakeRedstoneService { GetNodesFailure = new IOException("boom") };
        var viewModel = Create(service);

        SelectRedstone(viewModel);

        Assert.Empty(viewModel.RedstoneNodes);
        Assert.Equal(Strings.Redstone_NodesFailed, viewModel.RedstoneStatus);
        Assert.False(viewModel.StartRedstoneCommand.CanExecute(null));
    }

    private static void SelectRedstone(MultiplayerPageViewModel viewModel)
    {
        var section = viewModel.Sections.Single(
            item => item.Section is MultiplayerPageSection.Redstone);
        viewModel.SelectSectionCommand.Execute(section);
    }

    private static MultiplayerPageViewModel Create(IRedstoneOnlineService service) =>
        new(
            new StubLobbyService(),
            new StubClipboardService(),
            new InlineDispatcher(),
            new SilentMessages(),
            new SilentMessages(),
            redstoneService: service);

    private sealed class FakeRedstoneService : IRedstoneOnlineService
    {
        public Exception? GetNodesFailure { get; init; }

        public Exception? StartFailure { get; init; }

        public int GetNodesCount { get; private set; }

        public int StopCount { get; private set; }

        public int LastGamePort { get; private set; }

        public Task<IReadOnlyList<RedstoneNode>> GetNodesAsync(
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            GetNodesCount++;
            if (GetNodesFailure is not null)
                return Task.FromException<IReadOnlyList<RedstoneNode>>(GetNodesFailure);

            IReadOnlyList<RedstoneNode> nodes =
            [
                new("南京", "nj.hongshi.site"),
                new("成都", "cd.hongshi.site")
            ];
            return Task.FromResult(nodes);
        }

        public Task<RedstoneSession> StartAsync(
            int gamePort,
            RedstoneNode node,
            CancellationToken cancellationToken = default)
        {
            if (StartFailure is not null)
                return Task.FromException<RedstoneSession>(StartFailure);

            LastGamePort = gamePort;
            return Task.FromResult(new RedstoneSession($"{node.Address}:44812", gamePort, node.Address));
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubLobbyService : IMultiplayerLobbyService
    {
        public MultiplayerLobbySnapshot? Current => null;

#pragma warning disable CS0067
        public event Action<MultiplayerLobbySnapshot>? SnapshotChanged;
        public event Action<MultiplayerLobbyStopped>? Stopped;
#pragma warning restore CS0067

        public Task<MultiplayerLobbySnapshot> CreateHostAsync(
            string hostName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MultiplayerLobbySnapshot> JoinAsync(
            string roomCode,
            string playerName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubClipboardService : IClipboardService
    {
        public Task<bool> CopyTextAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<string?> GetTextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool HasAccess => true;

        public void Post(Action action) => action();

        public void PostAfterTransition(Action action) => action();

        public Task PostAfterTransitionAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public void Invoke(Action action) => action();

        public Task InvokeAsync(Func<Task> action) => action();
    }

    private sealed class SilentMessages : IStatusService, IFloatingMessageService
    {
        public event Action<string>? MessageReported;
        public event Action<FloatingMessageRequest>? MessageRequested;

        public string? LastMessage { get; private set; }

        public void Report(string message)
        {
            LastMessage = message;
            MessageReported?.Invoke(message);
        }

        public void Show(string message)
        {
            LastMessage = message;
            MessageRequested?.Invoke(new FloatingMessageRequest(message));
        }

        public void ShowDragHint(object source, string message) => Show(message);

        public void ClearDragHint(object source) => ClearDragHint();

        public void ClearDragHint() => MessageRequested?.Invoke(new FloatingMessageRequest(string.Empty));
    }
}
