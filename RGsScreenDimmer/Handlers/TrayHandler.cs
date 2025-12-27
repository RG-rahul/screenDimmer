using DimmerBeyond;
using DimmerBeyond.Handlers;
using System.ComponentModel;
using System.Reflection;

namespace ScreenDimmer.Handlers
{
    internal class TrayHandler
    {
        private NotifyIcon _notifyIcon = new();
        private ContextMenuStrip _contextMenu = new();
        private TrackBar _opacityTrackBar = new();
        private ToolStripControlHost? _trackBarHost;
        private readonly Action<double> _onOpacityChanged;
        private readonly Action<int> _updateOpacityPercentCache;
        private int _cachedOpacityPercent;
        private readonly CacheHandler _cacheHandler = new();

        public TrayHandler(Action<double> onOpacityChanged, Action<int> updateOpacityPercentCache, int cachedOpacityPercent)
        {
            _onOpacityChanged = onOpacityChanged;
            _updateOpacityPercentCache = updateOpacityPercentCache;
            _cachedOpacityPercent = cachedOpacityPercent;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            // Create the trackbar for opacity control
            _opacityTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 80,
                Value = _cachedOpacityPercent,
                TickStyle = TickStyle.None,
                Width = 150,
                Height = 30
            };
            _opacityTrackBar.ValueChanged += OnOpacityTrackBarValueChanged;

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.AutoClose = true;
            _contextMenu.Closed += ContextMenu_Closed;

            // Add opacity label
            _contextMenu.Items.Add(new ToolStripLabel($"Opacity: {_cachedOpacityPercent}%"));

            // Add trackbar to context menu
            _trackBarHost = new ToolStripControlHost(_opacityTrackBar);
            _contextMenu.Items.Add(_trackBarHost);

            // Add separator and exit option
            _contextMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClick);
            _contextMenu.Items.Add(exitItem);

            // Create notify icon
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.MouseClick += OnTrayIconMouseClick;
        }

        private Icon CreateTrayIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("DimmerBeyond.Resources.icon.ico");
            return new Icon(stream!);
        }

        private void OnOpacityTrackBarValueChanged(object? sender, EventArgs e)
        {
            double opacity = _opacityTrackBar.Value / 100.0;
            _onOpacityChanged(opacity);

            // Update the label and tooltip
            var label = (ToolStripLabel)_contextMenu.Items[0];
            label.Text = $"Opacity: {_opacityTrackBar.Value}%";
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
            if (_cachedOpacityPercent != _opacityTrackBar.Value)
            {
                _cachedOpacityPercent = _opacityTrackBar.Value;
                _updateOpacityPercentCache(_cachedOpacityPercent);
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void UpdateOpacity(double opacity)
        {
            int value = (int)(opacity * 100);
            if (value != _opacityTrackBar.Value)
            {
                _opacityTrackBar.Value = Math.Max(_opacityTrackBar.Minimum, Math.Min(_opacityTrackBar.Maximum, value));
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            _opacityTrackBar?.Dispose();
            if (_contextMenu != null)
            {
                _contextMenu.Closed -= ContextMenu_Closed;
            }
           
        }
    }
}