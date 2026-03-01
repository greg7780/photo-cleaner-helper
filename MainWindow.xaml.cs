using photo_cleaner_helper.ViewModels;
using SharpDX.XInput;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace photo_cleaner_helper
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();
        private readonly Controller _controller = new(UserIndex.One);
        private readonly DispatcherTimer _timer = new();
        private DateTime _lastInput = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _vm;

            VideoPlayer.MediaOpened += (s, e) => VideoPlayer.Play();

            PickFolder();

            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += PollGamepad;
            _timer.Start();

            KeyDown += MainWindow_KeyDown;
        }

        private void PickFolder()
        {
            using var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _vm.LoadFolder(dialog.SelectedPath);
            }
            else
            {
                Close();
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Right)
                _vm.Next();

            if (e.Key == System.Windows.Input.Key.Left)
                _vm.Previous();

            if (e.Key == System.Windows.Input.Key.X)
                _vm.DeleteCurrent();

            if (e.Key == System.Windows.Input.Key.Y)
                _vm.UndoDelete();
        }

        private void PollGamepad(object? sender, EventArgs e)
        {
            if (!_controller.IsConnected)
                return;

            if ((DateTime.Now - _lastInput).TotalMilliseconds < 300)
                return;

            var state = _controller.GetState();

            if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
            {
                _vm.Next();
                _lastInput = DateTime.Now;
            }

            if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
            {
                _vm.Previous();
                _lastInput = DateTime.Now;
            }

            if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
            {
                _vm.DeleteCurrent();
                _lastInput = DateTime.Now;
            }

            if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Y))
            {
                _vm.UndoDelete();
                _lastInput = DateTime.Now;
            }

            if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
            {
                if (_vm.IsCurrentVideo)
                {
                    _vm.IsPlaying = !_vm.IsPlaying;

                    if (_vm.IsPlaying)
                        VideoPlayer.Play();
                    else
                        VideoPlayer.Stop();

                    _lastInput = DateTime.Now;
                }
            }
        }
    }
}