﻿using System.Globalization;
using Content.Client.UserInterface.Controls;
using Content.Shared.CCVar;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Client.Stylesheets;

namespace Content.Client.Communications.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class CommunicationsConsoleMenu : FancyWindow
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;

        public bool CanAnnounce;
        public bool CanBroadcast;
        public bool CanCall;
        public bool AlertLevelSelectable;
        public bool CountdownStarted;
        public string CurrentLevel = string.Empty;
        public TimeSpan? CountdownEnd;

        public event Action? OnEmergencyLevel;
        public event Action<string>? OnAlertLevel;
        public event Action<string>? OnAnnounce;
        public event Action<string>? OnBroadcast;

        public event Action? OnCentcomm;
        public event Action? OnMaint;

        public CommunicationsConsoleMenu()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            MessageInput.Placeholder = new Rope.Leaf(_loc.GetString("comms-console-menu-announcement-placeholder"));

            MaintEmergencyButton.StyleClasses.Add(StyleBase.ButtonCaution); //nao funfa

            var maxAnnounceLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            MessageInput.OnTextChanged += (args) =>
            {
                if (args.Control.TextLength > maxAnnounceLength)
                {
                    AnnounceButton.Disabled = true;
                    AnnounceButton.ToolTip = Loc.GetString("comms-console-message-too-long");
                }
                else
                {
                    AnnounceButton.Disabled = !CanAnnounce;
                    AnnounceButton.ToolTip = null;

                }
            };

            AnnounceButton.OnPressed += _ => OnAnnounce?.Invoke(Rope.Collapse(MessageInput.TextRope));
            AnnounceButton.Disabled = !CanAnnounce;

            BroadcastButton.OnPressed += _ => OnBroadcast?.Invoke(Rope.Collapse(MessageInput.TextRope));
            BroadcastButton.Disabled = !CanBroadcast;

            AlertLevelButton.OnItemSelected += args =>
            {
                var metadata = AlertLevelButton.GetItemMetadata(args.Id);
                if (metadata != null && metadata is string cast)
                {
                    OnAlertLevel?.Invoke(cast);
                }
            };


            AlertLevelButton.Disabled = !AlertLevelSelectable;

            MaintEmergencyButton.OnPressed += _ => OnMaint?.Invoke();

            CentCommButton.OnPressed += _ => OnCentcomm?.Invoke();

            EmergencyShuttleButton.OnPressed += _ => OnEmergencyLevel?.Invoke();
            EmergencyShuttleButton.Disabled = !CanCall;

            UpdateCountdown();
        }

        // The current alert could make levels unselectable, so we need to ensure that the UI reacts properly.
        // If the current alert is unselectable, the only item in the alerts list will be
        // the current alert. Otherwise, it will be the list of alerts, with the current alert
        // selected.
        public void UpdateAlertLevels(List<string>? alerts, string currentAlert)
        {
            AlertLevelButton.Clear();

            if (alerts == null)
            {
                var name = currentAlert;
                if (Loc.TryGetString($"alert-level-{currentAlert}", out var locName))
                {
                    name = locName;
                }
                AlertLevelButton.AddItem(name);
                AlertLevelButton.SetItemMetadata(AlertLevelButton.ItemCount - 1, currentAlert);
            }
            else
            {
                foreach (var alert in alerts)
                {
                    var name = alert;
                    if (Loc.TryGetString($"alert-level-{alert}", out var locName))
                    {
                        name = locName;
                    }
                    AlertLevelButton.AddItem(name);
                    AlertLevelButton.SetItemMetadata(AlertLevelButton.ItemCount - 1, alert);
                    if (alert == currentAlert)
                    {
                        AlertLevelButton.Select(AlertLevelButton.ItemCount - 1);
                    }
                }
            }
        }

        public void UpdateCountdown()
        {
            if (!CountdownStarted)
            {
                CountdownLabel.SetMessage(string.Empty);
                EmergencyShuttleButton.Text = Loc.GetString("comms-console-menu-call-shuttle");
                return;
            }

            var diff = MathHelper.Max((CountdownEnd - _timing.CurTime) ?? TimeSpan.Zero, TimeSpan.Zero);

            EmergencyShuttleButton.Text = Loc.GetString("comms-console-menu-recall-shuttle");
            var infoText = Loc.GetString($"comms-console-menu-time-remaining",
                ("time", diff.ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture)));
            CountdownLabel.SetMessage(infoText);
        }
    }
}
