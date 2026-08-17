using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using DesktopPet.Native;
using DesktopPet.Pet;
using Application = System.Windows.Application;

namespace DesktopPet;

/// <summary>
/// The pet itself: a transparent, click-through, always-on-top window that walks the
/// taskbar. Everything species-specific comes from the <see cref="PetProfile"/> it is
/// constructed with, so each pet is just a profile plus its own sprite sheets.
/// </summary>
public partial class PetWindow : Window
{
    private const int TickMs = 130;

    private readonly PetProfile _profile;

    private SpriteAnimator _idleAnimator = null!;
    private SpriteAnimator _walkAnimator = null!;
    private SpriteAnimator? _specialAnimator;
    private PetStateMachine _pet = null!;
    private DispatcherTimer _timer = null!;
    private NotifyIcon? _trayIcon;
    private PetState _lastState = PetState.Idle;

    public PetWindow(PetProfile profile)
    {
        _profile = profile;
        InitializeComponent();

        Title = profile.DisplayName;
        Width = profile.FrameWidth;
        Height = profile.FrameHeight;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    /// <summary>
    /// Sprite sheets live in the *executable's* Assets/Sprites folder (no ";component"
    /// segment), so each pet exe ships only its own art even though this window lives
    /// in the shared library.
    /// </summary>
    private static Uri PackUri(string fileName) =>
        new($"pack://application:,,,/Assets/Sprites/{fileName}", UriKind.Absolute);

    private static SpriteAnimator Animator(SpriteSheet sheet) =>
        new(PackUri(sheet.FileName), sheet.FrameCount, sheet.TicksPerFrame);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _idleAnimator = Animator(_profile.Idle);
        _walkAnimator = Animator(_profile.Walk);
        _specialAnimator = _profile.Special is { } special ? Animator(special) : null;

        var hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeClickThrough(hwnd);

        var taskbar = TaskbarTracker.GetBounds();
        var startX = taskbar.Left + (taskbar.Width - Width) / 2;
        Left = startX;
        Top = taskbar.Top - Height + _profile.FeetSinkIntoTaskbar;

        _pet = new PetStateMachine(
            startX,
            _profile.WalkSpeedPxPerTick,
            specialTicks: _profile.Special?.LoopTicks ?? 0,
            specialChance: _profile.SpecialChance);
        PetImage.Source = _idleAnimator.CurrentFrame;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TickMs) };
        _timer.Tick += OnTick;
        _timer.Start();

        SetupTrayIcon();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var taskbar = TaskbarTracker.GetBounds();
        var minX = taskbar.Left;
        var maxX = taskbar.Left + taskbar.Width - Width;

        _pet.Tick(minX, maxX);
        Left = _pet.X;
        Top = taskbar.Top - Height + _profile.FeetSinkIntoTaskbar;

        FlipTransform.ScaleX = _pet.Facing == FacingDirection.Left ? -1 : 1;

        var animator = _pet.State switch
        {
            PetState.Walk => _walkAnimator,
            PetState.Special => _specialAnimator ?? _idleAnimator,
            _ => _idleAnimator,
        };

        if (_pet.State != _lastState)
        {
            animator.Reset();
            _lastState = _pet.State;
        }
        else
        {
            animator.Advance();
        }

        PetImage.Source = animator.CurrentFrame;
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = _profile.DisplayName,
        };

        var menu = new ContextMenuStrip();

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled(_profile),
        };
        startupItem.Click += (_, _) =>
        {
            if (!StartupManager.CanManageStartup(_profile))
            {
                startupItem.Checked = !startupItem.Checked;
                System.Windows.MessageBox.Show(
                    $"Start-with-Windows only works when running the built {_profile.ExeName}.exe directly, not via 'dotnet run'.",
                    _profile.DisplayName);
                return;
            }

            StartupManager.SetEnabled(_profile, startupItem.Checked);
        };
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());
        _trayIcon.ContextMenuStrip = menu;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer?.Stop();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
    }
}
