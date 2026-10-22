/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.App.Resources;
using Launcher.App.Services;
using Launcher.App.ViewModels.Account;
using Launcher.Application.Services;
using Launcher.Application.Services.Multiplayer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Launcher.App.ViewModels.Multiplayer;

public sealed partial class MultiplayerPageViewModel : ObservableObject
{
    private readonly AccountPageViewModel? accountPage;
    private readonly IMultiplayerLobbyService lobbyService;
    private readonly IClipboardService clipboardService;
    private readonly IUiDispatcher uiDispatcher;
    private readonly IStatusService statusService;
    private readonly IFloatingMessageService floatingMessageService;
    private readonly IExternalLinkService? externalLinkService;
    private readonly IRedstoneOnlineService? redstoneService;
    private MultiplayerSectionItem? lastTerracottaSection;
    private readonly ILogger<MultiplayerPageViewModel> logger;

    [ObservableProperty]
    private MultiplayerSectionItem? selectedSection;

    [ObservableProperty]
    private MultiplayerCreateLobbyStep createLobbyStep;

    [ObservableProperty]
    private string lobbyOwnerName = Strings.Multiplayer_LobbyOwnerPlaceholder;

    [ObservableProperty]
    private string roomCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinLobbyCommand))]
    private string joinRoomCode = string.Empty;

    [ObservableProperty]
    private bool isLobbyHost;

    [ObservableProperty]
    private bool isLeaveLobbyDialogOpen;

    [ObservableProperty]
    private bool isLobbySectionSwitchBlockedDialogOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateLobbyCommand))]
    private bool isCreatingLobby;

    [ObservableProperty]
    private bool isLanWorldDetectionDialogOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PasteRoomCodeCommand))]
    [NotifyCanExecuteChangedFor(nameof(JoinLobbyCommand))]
    private bool isJoiningLobby;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestLeaveLobbyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmLeaveLobbyCommand))]
    private bool isStoppingLobby;

    [ObservableProperty]
    private string joinLobbyStatus = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRedstoneCommand))]
    private RedstoneNode? selectedRedstoneNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRedstoneCommand))]
    private string redstoneGamePort = "25565";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshRedstoneNodesCommand))]
    private bool isLoadingRedstoneNodes;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRedstoneCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRedstoneCommand))]
    private bool isStartingRedstone;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRedstoneCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRedstoneCommand))]
    private bool isRedstoneRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyRedstoneAddressCommand))]
    private string redstoneAddress = string.Empty;

    [ObservableProperty]
    private string redstoneStatus = string.Empty;

    public MultiplayerPageViewModel(
        IMultiplayerLobbyService lobbyService,
        IClipboardService clipboardService,
        IUiDispatcher uiDispatcher,
        IStatusService statusService,
        IFloatingMessageService floatingMessageService,
        AccountPageViewModel? accountPage = null,
        IExternalLinkService? externalLinkService = null,
        ILogger<MultiplayerPageViewModel>? logger = null,
        IRedstoneOnlineService? redstoneService = null)
    {
        this.lobbyService = lobbyService;
        this.clipboardService = clipboardService;
        this.uiDispatcher = uiDispatcher;
        this.statusService = statusService;
        this.floatingMessageService = floatingMessageService;
        this.accountPage = accountPage;
        this.externalLinkService = externalLinkService;
        this.redstoneService = redstoneService;
        this.logger = logger ?? NullLogger<MultiplayerPageViewModel>.Instance;
        Sections =
        [
            new(MultiplayerPageSection.CreateLobby, Strings.Multiplayer_SectionCreateLobby, "multiple_player/multi_create"),
            new(MultiplayerPageSection.JoinLobby, Strings.Multiplayer_SectionJoinLobby, "multiple_player/multi_enter"),
            new(MultiplayerPageSection.Redstone, Strings.Redstone_SectionTitle, "server")
        ];
        SelectedSection = Sections[0];
        lobbyService.SnapshotChanged += OnLobbySnapshotChanged;
        lobbyService.Stopped += OnLobbyStopped;
    }

    public ObservableCollection<MultiplayerSectionItem> Sections { get; }

    public ObservableCollection<MultiplayerLobbyPlayerItem> LobbyPlayers { get; } = [];

    public ObservableCollection<RedstoneNode> RedstoneNodes { get; } = [];

    public string HeaderTitle => IsLobbyStep
        ? LobbyTitle
        : Strings.Page_Multiplayer;

    public bool IsTransportSwitchVisible => !IsLobbyStep;

    public bool IsCreateLobbySection => SelectedSection?.Section is MultiplayerPageSection.CreateLobby;

    public bool IsJoinLobbySection => SelectedSection?.Section is MultiplayerPageSection.JoinLobby;

    public bool IsRedstoneSection => SelectedSection?.Section is MultiplayerPageSection.Redstone;

    public bool IsTerracottaSection => IsCreateLobbySection || IsJoinLobbySection;

    public bool HasRedstoneStatus => !string.IsNullOrWhiteSpace(RedstoneStatus);

    public bool HasRedstoneSession => IsRedstoneRunning && !string.IsNullOrWhiteSpace(RedstoneAddress);

    public bool IsRedstoneEditable => !IsRedstoneRunning && !IsStartingRedstone;

    public string RedstoneSelectedNodeAddress => SelectedRedstoneNode?.Address ?? string.Empty;

    public string RedstoneStartButtonText => IsStartingRedstone
        ? Strings.Redstone_Starting
        : Strings.Redstone_Start;

    public bool IsLobbyStep => CreateLobbyStep is MultiplayerCreateLobbyStep.Lobby;

    public string LobbyTitle => string.Format(Strings.Multiplayer_LobbyTitleFormat, LobbyOwnerName);

    public string JoinLobbyButtonText => IsJoiningLobby
        ? Strings.Multiplayer_Join_Joining
        : Strings.Multiplayer_SectionJoinLobby;

    public bool HasJoinLobbyStatus => !string.IsNullOrWhiteSpace(JoinLobbyStatus);

    public string LeaveLobbyButtonText => IsLobbyHost
        ? Strings.Multiplayer_LobbyLeaveAndDisbandButton
        : Strings.Multiplayer_LobbyLeaveButton;

    public string LeaveLobbyDialogTitle => IsLobbyHost
        ? Strings.Dialog_MultiplayerLeaveLobbyTitle
        : Strings.Dialog_MultiplayerLeaveJoinedLobbyTitle;

    public string LeaveLobbyDialogMessage => IsLobbyHost
        ? Strings.Dialog_MultiplayerLeaveLobbyMessage
        : Strings.Dialog_MultiplayerLeaveJoinedLobbyMessage;

    public string LeaveLobbyConfirmButtonText => IsLobbyHost
        ? Strings.Dialog_MultiplayerLeaveLobbyConfirmButton
        : Strings.Dialog_MultiplayerLeaveJoinedLobbyConfirmButton;

    private bool CanCreateLobby => !IsCreatingLobby;

    private bool CanPasteRoomCode => !IsJoiningLobby;

    private bool CanJoinLobby => !string.IsNullOrWhiteSpace(JoinRoomCode)
        && !IsJoiningLobby;

    private bool CanRequestLeaveLobby => IsLobbyStep && !IsStoppingLobby;

    private bool CanConfirmLeaveLobby => IsLobbyStep && !IsStoppingLobby;

    private bool CanCopyRoomCode => !string.IsNullOrWhiteSpace(RoomCode);

    private bool CanCancelLobbyDetection => IsCreatingLobby;

    private bool CanRefreshRedstoneNodes => redstoneService is not null && !IsLoadingRedstoneNodes;

    private bool CanStartRedstone => redstoneService is not null
        && !IsStartingRedstone
        && !IsRedstoneRunning
        && SelectedRedstoneNode is not null
        && TryGetRedstonePort(out _);

    private bool CanStopRedstone => redstoneService is not null && IsRedstoneRunning && !IsStartingRedstone;

    private bool CanCopyRedstoneAddress => !string.IsNullOrWhiteSpace(RedstoneAddress);

    private bool TryGetRedstonePort(out int port) =>
        int.TryParse(RedstoneGamePort.Trim(), out port) && port is >= 1 and <= 65535;

    [RelayCommand]
    private void SelectSection(MultiplayerSectionItem? section)
    {
        if (section is null || section.Section == SelectedSection?.Section)
            return;

        if (IsLobbyStep)
        {
            IsLobbySectionSwitchBlockedDialogOpen = true;
            return;
        }

        SelectedSection = section;
    }

    [RelayCommand]
    private void SelectTransport(MultiplayerTransportKind transport)
    {
        var target = transport switch
        {
            MultiplayerTransportKind.Redstone => Sections.FirstOrDefault(
                item => item.Section is MultiplayerPageSection.Redstone),
            _ => lastTerracottaSection ?? Sections.FirstOrDefault(
                item => item.Section is MultiplayerPageSection.CreateLobby)
        };

        if (target is not null)
            SelectSection(target);
    }

    [RelayCommand]
    private void SelectTerracottaPage(MultiplayerPageSection page)
    {
        var target = Sections.FirstOrDefault(item => item.Section == page);
        if (target is not null)
            SelectSection(target);
    }

    [RelayCommand]
    private void CloseLobbySectionSwitchBlockedDialog()
    {
        IsLobbySectionSwitchBlockedDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanCreateLobby))]
    private async Task CreateLobbyAsync(CancellationToken cancellationToken)
    {
        IsCreatingLobby = true;
        IsLanWorldDetectionDialogOpen = true;
        try
        {
            var hostName = accountPage?.SelectedAccount?.DisplayName
                ?? Strings.Multiplayer_LobbyOwnerPlaceholder;
            var snapshot = await lobbyService.CreateHostAsync(hostName, cancellationToken);
            IsLobbyHost = true;
            ApplyLobbySnapshot(snapshot);
            CreateLobbyStep = MultiplayerCreateLobbyStep.Lobby;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (MultiplayerLobbyCreationException exception)
        {
            logger.LogWarning(exception,
                "Failed to create multiplayer lobby. Failure={Failure}",
                exception.Failure);
            ReportFailure(MapCreationFailure(exception.Failure));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to create multiplayer lobby.");
            ReportFailure(Strings.Multiplayer_Create_LobbyFailed);
        }
        finally
        {
            IsLanWorldDetectionDialogOpen = false;
            IsCreatingLobby = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelLobbyDetection))]
    private void CancelLobbyDetection()
    {
        IsLanWorldDetectionDialogOpen = false;
        CreateLobbyCommand.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanPasteRoomCode))]
    private async Task PasteRoomCodeAsync(CancellationToken cancellationToken)
    {
        string? clipboardText;
        try
        {
            clipboardText = await clipboardService.GetTextAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read a Terracotta room code from the clipboard.");
            ReportJoinFailure(Strings.Multiplayer_Join_ClipboardEmpty);
            return;
        }

        var normalized = clipboardText?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            ReportJoinFailure(Strings.Multiplayer_Join_ClipboardEmpty);
            return;
        }

        JoinRoomCode = normalized;
        JoinLobbyStatus = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanJoinLobby))]
    private async Task JoinLobbyAsync(CancellationToken cancellationToken)
    {
        var normalizedRoomCode = JoinRoomCode.Trim();
        if (normalizedRoomCode.Length == 0)
            return;

        IsJoiningLobby = true;
        JoinLobbyStatus = string.Empty;
        try
        {
            var playerName = accountPage?.SelectedAccount?.DisplayName
                ?? Strings.Multiplayer_LobbyOwnerPlaceholder;
            var snapshot = await lobbyService.JoinAsync(
                normalizedRoomCode,
                playerName,
                cancellationToken);
            IsLobbyHost = false;
            JoinRoomCode = snapshot.RoomCode;
            ApplyLobbySnapshot(snapshot);
            CreateLobbyStep = MultiplayerCreateLobbyStep.Lobby;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (MultiplayerLobbyCreationException exception)
        {
            logger.LogWarning(exception,
                "Failed to join multiplayer lobby. Failure={Failure}",
                exception.Failure);
            ReportJoinFailure(MapJoinFailure(exception.Failure));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to join multiplayer lobby.");
            ReportJoinFailure(Strings.Multiplayer_Join_Failed);
        }
        finally
        {
            IsJoiningLobby = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRequestLeaveLobby))]
    private void RequestLeaveLobby()
    {
        IsLeaveLobbyDialogOpen = true;
    }

    [RelayCommand]
    private void CancelLeaveLobby()
    {
        IsLeaveLobbyDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmLeaveLobby))]
    private async Task ConfirmLeaveLobbyAsync(CancellationToken cancellationToken)
    {
        IsLeaveLobbyDialogOpen = false;
        IsStoppingLobby = true;
        try
        {
            await lobbyService.StopAsync(cancellationToken);
            ResetLobbyView();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to stop multiplayer lobby cleanly.");
            ReportFailure(IsLobbyHost
                ? Strings.Multiplayer_LobbyDisbandFailed
                : Strings.Multiplayer_LobbyLeaveFailed);
        }
        finally
        {
            IsStoppingLobby = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyRoomCode))]
    private async Task CopyRoomCodeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await clipboardService.CopyTextAsync(RoomCode, cancellationToken))
            {
                statusService.Report(Strings.Multiplayer_LobbyRoomCodeCopied);
                floatingMessageService.Show(Strings.Multiplayer_LobbyRoomCodeCopied);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to copy the multiplayer room code.");
        }

        statusService.Report(Strings.Multiplayer_LobbyRoomCodeCopyFailed);
        floatingMessageService.Show(Strings.Multiplayer_LobbyRoomCodeCopyFailed);
    }

    [RelayCommand]
    private void OpenTerracottaProject()
    {
        try
        {
            if (externalLinkService?.TryOpen(TerracottaAgreementDialogViewModel.TerracottaProjectUrl) is true)
                return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to open the Terracotta project page from the multiplayer page.");
            ReportExternalLinkFailure();
            return;
        }

        logger.LogWarning("Failed to open the Terracotta project page from the multiplayer page.");
        ReportExternalLinkFailure();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshRedstoneNodes))]
    private Task RefreshRedstoneNodesAsync(CancellationToken cancellationToken) =>
        LoadRedstoneNodesAsync(forceRefresh: true, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanStartRedstone))]
    private async Task StartRedstoneAsync(CancellationToken cancellationToken)
    {
        if (redstoneService is null
            || SelectedRedstoneNode is null
            || !TryGetRedstonePort(out var gamePort))
        {
            return;
        }

        IsStartingRedstone = true;
        RedstoneStatus = string.Empty;
        try
        {
            var session = await redstoneService
                .StartAsync(gamePort, SelectedRedstoneNode, cancellationToken)
                .ConfigureAwait(true);
            RedstoneAddress = session.PublicAddress;
            IsRedstoneRunning = true;
            statusService.Report(Strings.Redstone_Started);
            floatingMessageService.Show(Strings.Redstone_Started);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to start the RedStone tunnel.");
            ReportRedstoneFailure(Strings.Redstone_StartFailed);
        }
        finally
        {
            IsStartingRedstone = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopRedstone))]
    private async Task StopRedstoneAsync(CancellationToken cancellationToken)
    {
        if (redstoneService is null)
            return;

        try
        {
            await redstoneService.StopAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to stop the RedStone tunnel.");
            ReportRedstoneFailure(Strings.Redstone_StopFailed);
        }
        finally
        {
            IsRedstoneRunning = false;
            RedstoneAddress = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyRedstoneAddress))]
    private async Task CopyRedstoneAddressAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await clipboardService.CopyTextAsync(RedstoneAddress, cancellationToken).ConfigureAwait(true))
            {
                statusService.Report(Strings.Redstone_AddressCopied);
                floatingMessageService.Show(Strings.Redstone_AddressCopied);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to copy the RedStone address.");
        }

        statusService.Report(Strings.Redstone_AddressCopyFailed);
        floatingMessageService.Show(Strings.Redstone_AddressCopyFailed);
    }

    private async Task LoadRedstoneNodesAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (redstoneService is null || IsLoadingRedstoneNodes)
            return;

        IsLoadingRedstoneNodes = true;
        try
        {
            var nodes = await redstoneService
                .GetNodesAsync(forceRefresh, cancellationToken)
                .ConfigureAwait(true);
            RedstoneNodes.Clear();
            foreach (var node in nodes)
                RedstoneNodes.Add(node);

            if (SelectedRedstoneNode is null || !RedstoneNodes.Contains(SelectedRedstoneNode))
                SelectedRedstoneNode = RedstoneNodes.FirstOrDefault();

            if (RedstoneNodes.Count == 0)
                ReportRedstoneFailure(Strings.Redstone_NodesEmpty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load the RedStone node list.");
            ReportRedstoneFailure(Strings.Redstone_NodesFailed);
        }
        finally
        {
            IsLoadingRedstoneNodes = false;
        }
    }

    private void OnLobbySnapshotChanged(MultiplayerLobbySnapshot snapshot)
    {
        uiDispatcher.Post(() => ApplyLobbySnapshot(snapshot));
    }

    private void OnLobbyStopped(MultiplayerLobbyStopped stopped)
    {
        uiDispatcher.Post(() =>
        {
            ResetLobbyView();
            var message = stopped.Reason switch
            {
                MultiplayerLobbyStopReason.MinecraftWorldClosed => Strings.Multiplayer_LobbyWorldClosed,
                MultiplayerLobbyStopReason.TerracottaExited => Strings.Multiplayer_LobbyTerracottaExited,
                MultiplayerLobbyStopReason.TerracottaServiceFailed => Strings.Multiplayer_LobbyTerracottaServiceFailed,
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(message))
                ReportFailure(message);
        });
    }

    private void ApplyLobbySnapshot(MultiplayerLobbySnapshot snapshot)
    {
        RoomCode = snapshot.RoomCode;
        LobbyOwnerName = snapshot.Players
            .FirstOrDefault(player => player.Kind is MultiplayerLobbyPlayerKind.Host)?.DisplayName
            ?? Strings.Multiplayer_LobbyOwnerPlaceholder;
        LobbyPlayers.Clear();
        for (var index = 0; index < snapshot.Players.Count; index++)
        {
            var player = snapshot.Players[index];
            LobbyPlayers.Add(new MultiplayerLobbyPlayerItem(
                player.DisplayName,
                player.Vendor,
                player.LatencyMilliseconds is { } latency
                    ? string.Format(Strings.Multiplayer_LobbyLatencyFormat, latency)
                    : Strings.Multiplayer_LobbyLatencyUnknown,
                player.Kind is MultiplayerLobbyPlayerKind.Host
                    ? Strings.Multiplayer_LobbyPlayerRoleHost
                    : Strings.Multiplayer_LobbyPlayerRolePlayer,
                player.Kind is MultiplayerLobbyPlayerKind.Host,
                player.IsLocal,
                index == 0,
                index == snapshot.Players.Count - 1));
        }
    }

    private void ResetLobbyView()
    {
        CreateLobbyStep = MultiplayerCreateLobbyStep.Setup;
        IsLanWorldDetectionDialogOpen = false;
        IsLeaveLobbyDialogOpen = false;
        IsLobbySectionSwitchBlockedDialogOpen = false;
        IsLobbyHost = false;
        RoomCode = string.Empty;
        LobbyPlayers.Clear();
    }

    private void ReportFailure(string message)
    {
        if (IsJoinLobbySection)
            JoinLobbyStatus = message;
        statusService.Report(message);
        floatingMessageService.Show(message);
    }

    private void ReportJoinFailure(string message)
    {
        JoinLobbyStatus = message;
        statusService.Report(message);
        floatingMessageService.Show(message);
    }

    private void ReportRedstoneFailure(string message)
    {
        RedstoneStatus = message;
        statusService.Report(message);
        floatingMessageService.Show(message);
    }

    private void ReportExternalLinkFailure()
    {
        statusService.Report(Strings.Status_OpenTerracottaProjectFailed);
        floatingMessageService.Show(Strings.Status_OpenTerracottaProjectFailed);
    }

    private static string MapCreationFailure(MultiplayerLobbyCreationFailure failure)
    {
        return failure switch
        {
            MultiplayerLobbyCreationFailure.TerracottaUnavailable => Strings.Multiplayer_Create_TerracottaUnavailable,
            MultiplayerLobbyCreationFailure.MinecraftWorldUnavailable => Strings.Multiplayer_Create_WorldUnavailable,
            MultiplayerLobbyCreationFailure.TerracottaBusy => Strings.Multiplayer_Create_TerracottaBusy,
            MultiplayerLobbyCreationFailure.TerracottaProtocolFailed => Strings.Multiplayer_Create_TerracottaProtocolFailed,
            _ => Strings.Multiplayer_Create_LobbyFailed
        };
    }

    private static string MapJoinFailure(MultiplayerLobbyCreationFailure failure)
    {
        return failure switch
        {
            MultiplayerLobbyCreationFailure.InvalidRoomCode => Strings.Multiplayer_Join_InvalidRoomCode,
            MultiplayerLobbyCreationFailure.TerracottaUnavailable => Strings.Multiplayer_Create_TerracottaUnavailable,
            MultiplayerLobbyCreationFailure.TerracottaBusy => Strings.Multiplayer_Create_TerracottaBusy,
            MultiplayerLobbyCreationFailure.TerracottaProtocolFailed => Strings.Multiplayer_Create_TerracottaProtocolFailed,
            _ => Strings.Multiplayer_Join_Failed
        };
    }

    partial void OnCreateLobbyStepChanged(MultiplayerCreateLobbyStep value)
    {
        if (value is not MultiplayerCreateLobbyStep.Lobby)
            IsLeaveLobbyDialogOpen = false;

        OnPropertyChanged(nameof(IsLobbyStep));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(IsTransportSwitchVisible));
        RequestLeaveLobbyCommand.NotifyCanExecuteChanged();
        ConfirmLeaveLobbyCommand.NotifyCanExecuteChanged();
    }

    partial void OnLobbyOwnerNameChanged(string value)
    {
        OnPropertyChanged(nameof(LobbyTitle));
        OnPropertyChanged(nameof(HeaderTitle));
    }

    partial void OnRoomCodeChanged(string value)
    {
        CopyRoomCodeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsCreatingLobbyChanged(bool value)
    {
        CancelLobbyDetectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsJoiningLobbyChanged(bool value)
    {
        OnPropertyChanged(nameof(JoinLobbyButtonText));
    }

    partial void OnIsLobbyHostChanged(bool value)
    {
        OnPropertyChanged(nameof(LeaveLobbyButtonText));
        OnPropertyChanged(nameof(LeaveLobbyDialogTitle));
        OnPropertyChanged(nameof(LeaveLobbyDialogMessage));
        OnPropertyChanged(nameof(LeaveLobbyConfirmButtonText));
    }

    partial void OnJoinLobbyStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasJoinLobbyStatus));
    }

    partial void OnSelectedSectionChanged(MultiplayerSectionItem? value)
    {
        foreach (var section in Sections)
            section.IsSelected = ReferenceEquals(section, value);

        OnPropertyChanged(nameof(IsCreateLobbySection));
        OnPropertyChanged(nameof(IsJoinLobbySection));
        OnPropertyChanged(nameof(IsRedstoneSection));
        OnPropertyChanged(nameof(IsTerracottaSection));

        // 记住陶瓦侧停在哪个页，切回陶瓦时回到原位。
        if (value is not null && IsTerracottaSection)
            lastTerracottaSection = value;

        if (IsRedstoneSection && RedstoneNodes.Count == 0)
            _ = LoadRedstoneNodesAsync(forceRefresh: false, CancellationToken.None);
    }

    partial void OnSelectedRedstoneNodeChanged(RedstoneNode? value)
    {
        OnPropertyChanged(nameof(RedstoneSelectedNodeAddress));
    }

    partial void OnIsStartingRedstoneChanged(bool value)
    {
        OnPropertyChanged(nameof(RedstoneStartButtonText));
        OnPropertyChanged(nameof(IsRedstoneEditable));
    }

    partial void OnIsRedstoneRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(HasRedstoneSession));
        OnPropertyChanged(nameof(IsRedstoneEditable));
    }

    partial void OnRedstoneAddressChanged(string value)
    {
        OnPropertyChanged(nameof(HasRedstoneSession));
    }

    partial void OnRedstoneStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasRedstoneStatus));
    }

}
