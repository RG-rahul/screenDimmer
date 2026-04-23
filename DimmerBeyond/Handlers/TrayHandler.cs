using System.Reflection;
using DimmerBeyond.Records;

namespace ScreenDimmer.Handlers
{
    internal class TrayHandler
    {
        private NotifyIcon _notifyIcon = new();
        private ContextMenuStrip _contextMenu = new();
        private readonly Action<string, double> _onOpacityChanged;
        private readonly Action<Dictionary<string, ScreenDimmerSettings>> _updateScreenSettingsCache;
        private readonly Dictionary<string, ScreenDimmerSettings> _screenSettingsByDeviceName;
        private readonly Dictionary<string, bool> _opacityEnabledByScreen = new();
        private readonly Dictionary<string, TrackBar> _trackBarsByScreen = new();
        private readonly Dictionary<string, Label> _labelsByScreen = new();
        private readonly IReadOnlyList<Screen> _screens;

        public TrayHandler(
            IReadOnlyList<Screen> screens,
            Action<string, double> onOpacityChanged,
            Action<Dictionary<string, ScreenDimmerSettings>> updateScreenSettingsCache,
            Dictionary<string, ScreenDimmerSettings> screenSettingsByDeviceName)
        {
            _screens = screens;
            _onOpacityChanged = onOpacityChanged;
            _updateScreenSettingsCache = updateScreenSettingsCache;
            _screenSettingsByDeviceName = screenSettingsByDeviceName;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.AutoClose = true;
            _contextMenu.Closed += ContextMenu_Closed;

            for (int i = 0; i < _screens.Count; i++)
            {
                AddScreenControls(_screens[i], i + 1);
            }

            _contextMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClick);
            _contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.MouseClick += OnTrayIconMouseClick;
        }

        private void AddScreenControls(Screen screen, int displayIndex)
        {
            var screenKey = screen.DeviceName;
            var displayName = $"Display {displayIndex}";
            var opacityValue = _screenSettingsByDeviceName[screenKey].OpacityPercent;
            var opacityEnabled = _screenSettingsByDeviceName[screenKey].Enabled;

            var displayHeaderItem = new ToolStripMenuItem(displayName)
            {
                Enabled = false
            };

            var opacityPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var opacityCheckBox = new CheckBox
            {
                Checked = opacityEnabled,
                Margin = new Padding(0, 0, 4, 0),
                AutoSize = true,
                Tag = screenKey
            };
            opacityCheckBox.CheckedChanged += OnOpacityCheckBoxChanged;
            _opacityEnabledByScreen[screenKey] = opacityEnabled;

            var opacityLabel = new Label
            {
                Text = $"Opacity: {opacityValue}%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _labelsByScreen[screenKey] = opacityLabel;

            opacityPanel.Controls.Add(opacityCheckBox);
            opacityPanel.Controls.Add(opacityLabel);
            var opacityHost = new ToolStripControlHost(opacityPanel);

            var opacityTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 80,
                Value = opacityValue,
                TickStyle = TickStyle.None,
                Width = 150,
                AutoSize = false,
                Height = 22,
                Tag = screenKey
            };
            opacityTrackBar.ValueChanged += OnOpacityTrackBarValueChanged;
            _trackBarsByScreen[screenKey] = opacityTrackBar;
            opacityTrackBar.Enabled = opacityEnabled;

            var trackBarHost = new ToolStripControlHost(opacityTrackBar);

            _contextMenu.Items.Add(displayHeaderItem);
            _contextMenu.Items.Add(opacityHost);
            _contextMenu.Items.Add(trackBarHost);
            _contextMenu.Items.Add(new ToolStripSeparator());
        }

        private Icon CreateTrayIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("DimmerBeyond.Resources.icon.ico");
            return new Icon(stream!);
        }

        private void OnOpacityTrackBarValueChanged(object? sender, EventArgs e)
        {
            if (sender is not TrackBar trackBar || trackBar.Tag is not string screenKey)
            {
                return;
            }

            if (_opacityEnabledByScreen[screenKey])
            {
                SetOpacity(screenKey, trackBar.Value);
            }

            _labelsByScreen[screenKey].Text = $"Opacity: {trackBar.Value}%";
        }

        private void OnOpacityCheckBoxChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.Tag is not string screenKey)
            {
                return;
            }

            _opacityEnabledByScreen[screenKey] = checkBox.Checked;
            _trackBarsByScreen[screenKey].Enabled = checkBox.Checked;

            if (checkBox.Checked)
            {
                SetOpacity(screenKey, _trackBarsByScreen[screenKey].Value);
            }
            else
            {
                SetOpacity(screenKey, 0);
            }
        }

        private void SetOpacity(string screenKey, int value)
        {
            double opacity = value / 100.0;
            _onOpacityChanged(screenKey, opacity);
        }

        private void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Use reflection to call the internal ShowContextMenu method
                MethodInfo? mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi?.Invoke(_notifyIcon, null);
            }
        }

        private void ContextMenu_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            bool hasChanges = false;

            foreach (var entry in _trackBarsByScreen)
            {
                var screenKey = entry.Key;
                var opacityValue = entry.Value.Value;
                if (_screenSettingsByDeviceName[screenKey].OpacityPercent != opacityValue)
                {
                    _screenSettingsByDeviceName[screenKey].OpacityPercent = opacityValue;
                    hasChanges = true;
                }

                var opacityEnabled = _opacityEnabledByScreen[screenKey];
                if (_screenSettingsByDeviceName[screenKey].Enabled != opacityEnabled)
                {
                    _screenSettingsByDeviceName[screenKey].Enabled = opacityEnabled;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                _updateScreenSettingsCache(_screenSettingsByDeviceName);
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            if (_contextMenu != null)
            {
                _contextMenu.Closed -= ContextMenu_Closed;
            }

            foreach (var trackBar in _trackBarsByScreen.Values)
            {
                trackBar.Dispose();
            }
        }
    }
}