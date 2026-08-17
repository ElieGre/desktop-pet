using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using DesktopPet.Native;
using DesktopPet.Pet;
using Application = System.Windows.Application;

namespace DesktopPet;

public partial class MainWindow : Window
{
    private const int TickMs = 130;
    private const int FeetSinkIntoTaskbar = 0; // negative raises the pet above the taskbar's top edge

    private SpriteAnimator _idleAnimator = null!;
    private SpriteAnimator _walkAnimator = null!;
    private PetStateMachine _pet = null!;
    private DispatcherTimer _timer = null!;
    private NotifyIcon? _trayIcon;
    private PetState _lastState = PetState.Idle;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private static Uri PackUri(string fileName) =>
        new($"pack://application:,,,/Assets/Sprites/{fileName}", UriKind.Absolute);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _idleAnimator = new SpriteAnimator(PackUri("croc_idle.png"), frameCount: 4, ticksPerFrame: 7);
        _walkAnimator = new SpriteAnimator(PackUri("croc_walk.png"), frameCount: 4, ticksPerFrame: 2);

        var hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeClickThrough(hwnd);

        var taskbar = TaskbarTracker.GetBounds();
        var startX = taskbar.Left + (taskbar.Width - Width) / 2;
        Left = startX;
        Top = taskbar.Top - Height + FeetSinkIntoTaskbar;

        _pet = new PetStateMachine(startX);
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
        Top = taskbar.Top - Height + FeetSinkIntoTaskbar;

        FlipTransform.ScaleX = _pet.Facing == FacingDirection.Left ? -1 : 1;

        var animator = _pet.State == PetState.Walk ? _walkAnimator : _idleAnimator;
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
            Text = "Desktop Pet Crocodile",
        };

        var menu = new ContextMenuStrip();

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled(),
        };
        startupItem.Click += (_, _) =>
        {
            if (!StartupManager.CanManageStartup())
            {
                startupItem.Checked = !startupItem.Checked;
                System.Windows.MessageBox.Show(
                    "Start-with-Windows only works when running the built DesktopPet.exe directly, not via 'dotnet run'.",
                    "Desktop Pet Crocodile");
                return;
            }

            StartupManager.SetEnabled(startupItem.Checked);
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
